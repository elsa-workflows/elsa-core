using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.Connections.Services;

/// <summary>Coordinates one-time provider refreshes through durable intent, provider-call, stage, and publish states.</summary>
public sealed class DefaultConnectionLifecycleService(
    IConnectionLifecycleStore store,
    IConnectionUseAuthorizer authorizer,
    IConnectionCredentialProvider provider,
    IManagedSecretManager secrets,
    ITenantAccessor tenantAccessor,
    TimeProvider timeProvider) : IConnectionLifecycleService, IConnectionBackgroundUseService, IConnectionLifecycleRecoveryService
{
    private static readonly TimeSpan OperationLeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ClaimsPrincipal SystemPrincipal = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "elsa-connections-lifecycle"), new Claim("elsa:identity-kind", "system")],
        "Elsa.Connections.Server"));

    public async Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, request.TenantId, request.EnvironmentId, "", "manage:connect", cancellationToken))
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.EnvironmentId) ||
            string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ProviderAccountId) ||
            string.IsNullOrWhiteSpace(request.InitialCredentials.AccessToken) || string.IsNullOrWhiteSpace(request.InitialCredentials.RefreshToken) ||
            request.InitialCredentials.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
            return new ConnectionLifecycleResult(false, "connection_input_invalid", null);

        using var tenantContext = PushTenant(request.TenantId);
        var connectionId = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid().ToString("N");
        var secretName = ManagedSecretNames.ForGeneration(connectionId, operationId);
        var connection = new IntegrationConnection
        {
            Id = connectionId,
            TenantId = request.TenantId,
            EnvironmentId = request.EnvironmentId,
            ProviderId = request.ProviderId,
            ProviderAccountId = request.ProviderAccountId,
            Status = ConnectionStatus.Active,
            Revision = 1,
            OperationId = operationId,
            OperationExpectedRevision = 1,
            OperationFence = 1,
            OperationLeaseExpiresAt = timeProvider.GetUtcNow() + OperationLeaseDuration,
            OperationStatus = CredentialOperationStatus.CredentialReceived,
            PlannedSecretName = secretName,
            PlannedGenerationId = operationId
        };

        // Persist owner metadata and planned generation before encrypted material so a process crash is recoverable.
        try
        {
            await store.CreateAsync(connection, cancellationToken);
        }
        catch
        {
            return new ConnectionLifecycleResult(false, "connection_create_unknown", null, connectionId);
        }
        try
        {
            await secrets.CreateGenerationAsync(connectionId, operationId, Serialize(request.InitialCredentials), cancellationToken);
            if (!await store.TryRecordStagedGenerationAsync(connectionId, request.TenantId, request.EnvironmentId, 1, operationId, 1, secretName, operationId, cancellationToken) ||
                !await store.TryPublishGenerationAsync(connectionId, request.TenantId, request.EnvironmentId, 1, operationId, 1, cancellationToken))
            {
                await TryMarkRecoveryRequiredAsync(connection, request.TenantId, request.EnvironmentId, "connection_publish_conflict");
                return new ConnectionLifecycleResult(false, "connection_publish_conflict", 1, connectionId);
            }
        }
        catch
        {
            await TryMarkRecoveryRequiredAsync(connection, request.TenantId, request.EnvironmentId, "connection_outcome_unknown");
            return new ConnectionLifecycleResult(false, "connection_outcome_unknown", 1, connectionId);
        }

        return new ConnectionLifecycleResult(true, null, 2, connectionId);
    }

    public async Task<ConnectionAccessCredential> ResolveForUseAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "use", cancellationToken))
            throw new ConnectionUnavailableException();

        return await ResolveAuthorizedCredentialAsync(tenantId, environmentId, connectionId, cancellationToken);
    }

    public async Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(SystemPrincipal, ConnectionUseKind.BackgroundSystem, tenantId, environmentId, connectionId, "use", cancellationToken))
            throw new ConnectionUnavailableException();

        return await ResolveAuthorizedCredentialAsync(tenantId, environmentId, connectionId, cancellationToken);
    }

    private async Task<ConnectionAccessCredential> ResolveAuthorizedCredentialAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken)
    {
        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (!CanUseCurrentGeneration(connection))
            throw new ConnectionUnavailableException();

        CredentialMaterial? material;
        try
        {
            var payload = await secrets.ResolveGenerationAsync(connection!.CurrentSecretName!, connection.Id, connection.CurrentGenerationId!, cancellationToken);
            material = Deserialize(payload.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Connection access was cancelled.", cancellationToken);
        }
        catch
        {
            throw new ConnectionUnavailableException();
        }

        if (material == null || material.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
            throw new ConnectionUnavailableException();

        return new ConnectionAccessCredential(material.AccessToken, material.AccessTokenExpiresAt);
    }

    public async Task<ConnectionLifecycleResult> RefreshAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:refresh", cancellationToken))
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);

        using var tenantContext = PushTenant(tenantId);
        var current = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (!CanUseCurrentGeneration(current))
            return new ConnectionLifecycleResult(false, "connection_unavailable", current?.Revision);

        var operationId = Guid.NewGuid().ToString("N");
        var claimed = await store.TryClaimRefreshAsync(connectionId, tenantId, environmentId, current!.Revision, operationId, timeProvider.GetUtcNow() + OperationLeaseDuration, cancellationToken);
        if (claimed == null)
            return new ConnectionLifecycleResult(false, "refresh_conflict", current.Revision);

        var expectedRevision = claimed.OperationExpectedRevision;
        var fence = claimed.OperationFence;
        try
        {
            var oldPayload = await secrets.ResolveGenerationAsync(claimed.CurrentSecretName!, claimed.Id, claimed.CurrentGenerationId!, cancellationToken);
            var oldMaterial = Deserialize(oldPayload.Value);
            if (oldMaterial == null || string.IsNullOrWhiteSpace(oldMaterial.RefreshToken))
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "credential_unavailable");

            // Persist this edge before crossing the provider boundary. After it, no worker may replay the token.
            if (!await store.TryStartProviderCallAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken))
                return new ConnectionLifecycleResult(false, "refresh_conflict", claimed.Revision);

            var refreshed = await provider.RefreshAsync(claimed.ProviderId, claimed.ProviderAccountId, oldMaterial.RefreshToken, cancellationToken);
            if (refreshed == null || string.IsNullOrWhiteSpace(refreshed.RefreshToken) || string.IsNullOrWhiteSpace(refreshed.AccessToken))
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "provider_refresh_unknown");

            var nextName = ManagedSecretNames.ForGeneration(connectionId, operationId);
            var encryptedEnvelope = Serialize(refreshed);
            await secrets.CreateGenerationAsync(connectionId, operationId, encryptedEnvelope, cancellationToken);

            if (!await store.TryRecordStagedGenerationAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, nextName, operationId, cancellationToken))
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "credential_stage_unknown");

            if (!await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken))
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "generation_publish_conflict");

            return new ConnectionLifecycleResult(true, null, expectedRevision + 1);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "refresh_outcome_unknown");
            throw new OperationCanceledException("Credential refresh was cancelled; provider outcome is unknown.", cancellationToken);
        }
        catch
        {
            // Provider/network/persistence errors after claiming can hide a one-time refresh-token rotation.
            await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "refresh_outcome_unknown");
            return new ConnectionLifecycleResult(false, "refresh_outcome_unknown", expectedRevision);
        }
    }

    public async Task<ConnectionLifecycleResult> ReconcileAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(SystemPrincipal, ConnectionUseKind.BackgroundSystem, tenantId, environmentId, connectionId, "manage:reconcile", cancellationToken))
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);

        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (connection == null)
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);

        if (connection.OperationStatus is CredentialOperationStatus.None or CredentialOperationStatus.Completed)
            return new ConnectionLifecycleResult(true, null, connection.Revision);

        if (connection.OperationStatus is CredentialOperationStatus.Claimed or CredentialOperationStatus.ProviderCallStarted or CredentialOperationStatus.CredentialReceived)
        {
            var expired = await store.TryMarkRecoveryRequiredIfLeaseExpiredAsync(connection.Id, tenantId, environmentId, connection.OperationId!, connection.OperationFence, timeProvider.GetUtcNow(), "refresh_outcome_unknown", cancellationToken);
            if (expired)
                return new ConnectionLifecycleResult(false, "refresh_outcome_unknown", connection.Revision + (connection.Status == ConnectionStatus.Active ? 1 : 0));

            return new ConnectionLifecycleResult(false, "operation_in_progress", connection.Revision);
        }

        if (connection.OperationStatus == CredentialOperationStatus.Staged)
        {
            if (connection.OperationLeaseExpiresAt > timeProvider.GetUtcNow())
                return new ConnectionLifecycleResult(false, "operation_in_progress", connection.Revision);

            if (await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, connection.OperationExpectedRevision, connection.OperationId!, connection.OperationFence, cancellationToken))
                return new ConnectionLifecycleResult(true, null, connection.OperationExpectedRevision + 1);

            await TryMarkRecoveryRequiredAsync(connection, tenantId, environmentId, "generation_publish_conflict");
            return new ConnectionLifecycleResult(false, "generation_publish_conflict", connection.Revision + (connection.Status == ConnectionStatus.Active ? 1 : 0));
        }

        if (connection.OperationStatus == CredentialOperationStatus.RecoveryRequired && connection.Status == ConnectionStatus.RecoveryRequired &&
            !string.IsNullOrWhiteSpace(connection.PlannedSecretName) && !string.IsNullOrWhiteSpace(connection.PlannedGenerationId))
        {
            try
            {
                var payload = await secrets.ResolveGenerationAsync(connection.PlannedSecretName, connection.Id, connection.PlannedGenerationId, cancellationToken);
                if (Deserialize(payload.Value) is not null && await store.TryPromoteRecoveryGenerationAsync(connectionId, tenantId, environmentId, connection.Revision, connection.OperationId!, connection.OperationFence, cancellationToken))
                    return new ConnectionLifecycleResult(true, null, connection.Revision + 1);
            }
            catch
            {
                // Missing or invalid planned material is not safe to publish; retain RecoveryRequired.
            }
        }

        return new ConnectionLifecycleResult(false, "recovery_required", connection.Revision);
    }

    private async Task<bool> AuthorizeAsync(ClaimsPrincipal principal, ConnectionUseKind kind, string tenantId, string environmentId, string connectionId, string purpose, CancellationToken cancellationToken) =>
        await authorizer.AuthorizeAsync(new ConnectionUseRequest(principal, kind, tenantId, environmentId, connectionId, purpose), cancellationToken);

    private static bool CanUseCurrentGeneration(IntegrationConnection? connection) =>
        connection is { Status: ConnectionStatus.Active, OperationStatus: CredentialOperationStatus.None or CredentialOperationStatus.Completed } &&
        !string.IsNullOrWhiteSpace(connection.CurrentSecretName) && !string.IsNullOrWhiteSpace(connection.CurrentGenerationId);

    private IDisposable PushTenant(string tenantId) => tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

    private static string Serialize(CredentialMaterial material) => JsonSerializer.Serialize(
        new CredentialEnvelope(material.AccessToken, material.RefreshToken, material.AccessTokenExpiresAt), JsonOptions);

    private static CredentialMaterial? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var envelope = JsonSerializer.Deserialize<CredentialEnvelope>(json, JsonOptions);
            return envelope == null ? null : new CredentialMaterial(envelope.AccessToken, envelope.RefreshToken, envelope.AccessTokenExpiresAt);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<ConnectionLifecycleResult> RequireRecoveryAsync(IntegrationConnection connection, string tenantId, string environmentId, string code)
    {
        await TryMarkRecoveryRequiredAsync(connection, tenantId, environmentId, code);
        return new ConnectionLifecycleResult(false, code, connection.OperationExpectedRevision);
    }

    private async Task TryMarkRecoveryRequiredAsync(IntegrationConnection connection, string tenantId, string environmentId, string code)
    {
        try
        {
            await store.MarkRecoveryRequiredAsync(connection.Id, tenantId, environmentId, connection.OperationId!, connection.OperationFence, code, CancellationToken.None);
        }
        catch
        {
            // Durable provider-call intent remains for startup reconciliation; never replay automatically.
        }
    }

    private sealed record CredentialEnvelope(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
}
