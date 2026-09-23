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
    TimeProvider timeProvider,
    ITenantAccessor tenantAccessor) : IConnectionLifecycleService, IConnectionBackgroundUseService, IConnectionLifecycleRecoveryService
{
    private static readonly TimeSpan OperationLeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ClaimsPrincipal SystemPrincipal = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "elsa-connections-lifecycle"), new Claim("elsa:identity-kind", "system")],
        "Elsa.Connections.Server"));

    public async Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, request.TenantId, request.EnvironmentId, "", "manage:connect", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.EnvironmentId) ||
            string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ProviderAccountId) ||
            string.IsNullOrWhiteSpace(request.InitialCredentials.AccessToken) || string.IsNullOrWhiteSpace(request.InitialCredentials.RefreshToken) ||
            request.InitialCredentials.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
        {
            return new ConnectionLifecycleResult(false, "connection_input_invalid", null);
        }

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryMarkRecoveryRequiredAsync(connection, request.TenantId, request.EnvironmentId, "connection_outcome_unknown");
            throw new OperationCanceledException("Connection setup was cancelled; creation outcome is unknown.", cancellationToken);
        }
        catch (Exception)
        {
            await TryMarkRecoveryRequiredAsync(connection, request.TenantId, request.EnvironmentId, "connection_outcome_unknown");
            return new ConnectionLifecycleResult(false, "connection_outcome_unknown", 1, connectionId);
        }

        connection.CurrentSecretName = secretName;
        connection.CurrentGenerationId = operationId;
        connection.OperationStatus = CredentialOperationStatus.Completed;
        connection.Revision = 2;
        return new ConnectionLifecycleResult(true, null, connection.Revision, connectionId, ToMetadata(connection));
    }

    public async Task<ConnectionAccessCredential> ResolveForUseAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "use", cancellationToken))
        {
            throw new ConnectionUnavailableException();
        }

        return await ResolveAuthorizedCredentialAsync(tenantId, environmentId, connectionId, cancellationToken);
    }

    public async Task<ConnectionAccessCredential> ResolveForUseAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(SystemPrincipal, ConnectionUseKind.BackgroundSystem, tenantId, environmentId, connectionId, "use", cancellationToken))
        {
            throw new ConnectionUnavailableException();
        }

        return await ResolveAuthorizedCredentialAsync(tenantId, environmentId, connectionId, cancellationToken);
    }

    private async Task<ConnectionAccessCredential> ResolveAuthorizedCredentialAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken)
    {
        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (!CanUseCurrentGeneration(connection))
        {
            throw new ConnectionUnavailableException();
        }

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
        catch (Exception)
        {
            throw new ConnectionUnavailableException();
        }

        if (material == null || material.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new ConnectionUnavailableException();
        }

        return new ConnectionAccessCredential(material.AccessToken, material.AccessTokenExpiresAt);
    }

    public async Task<ConnectionLifecycleResult> RefreshAsync(ClaimsPrincipal principal, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:refresh", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        using var tenantContext = PushTenant(tenantId);
        var current = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (current == null)
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }
        if (!CanUseCurrentGeneration(current))
        {
            var code = current.Status == ConnectionStatus.Active ? "refresh_conflict" : "connection_unavailable";
            return new ConnectionLifecycleResult(false, code, current.Revision, connectionId, ToMetadata(current));
        }

        var operationId = Guid.NewGuid().ToString("N");
        var claimed = await store.TryClaimRefreshAsync(connectionId, tenantId, environmentId, current!.Revision, operationId, timeProvider.GetUtcNow() + OperationLeaseDuration, cancellationToken);
        if (claimed == null)
        {
            // Another worker may already have claimed or completed the refresh. Return state reloaded after the
            // lost CAS instead of reporting the revision from this worker's stale pre-claim snapshot.
            var latest = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return latest == null
                ? new ConnectionLifecycleResult(false, "connection_unavailable", null)
                : new ConnectionLifecycleResult(false, "refresh_conflict", latest.Revision, connectionId, ToMetadata(latest));
        }

        var expectedRevision = claimed.OperationExpectedRevision;
        var fence = claimed.OperationFence;
        var providerCallStarted = false;
        try
        {
            var oldPayload = await secrets.ResolveGenerationAsync(claimed.CurrentSecretName!, claimed.Id, claimed.CurrentGenerationId!, cancellationToken);
            var oldMaterial = Deserialize(oldPayload.Value);
            if (oldMaterial == null || string.IsNullOrWhiteSpace(oldMaterial.RefreshToken))
            {
                return await ReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "credential_unavailable");
            }

            // Persist this edge before crossing the provider boundary. After it, no worker may replay the token.
            if (!await store.TryStartProviderCallAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, timeProvider.GetUtcNow(), cancellationToken))
            {
                return await ReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "refresh_conflict");
            }
            providerCallStarted = true;

            var refreshed = await provider.RefreshAsync(claimed.ProviderId, claimed.ProviderAccountId, oldMaterial.RefreshToken, cancellationToken);
            if (refreshed == null || string.IsNullOrWhiteSpace(refreshed.RefreshToken) || string.IsNullOrWhiteSpace(refreshed.AccessToken))
            {
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "provider_refresh_unknown");
            }

            var nextName = ManagedSecretNames.ForGeneration(connectionId, operationId);
            var encryptedEnvelope = Serialize(refreshed);
            await secrets.CreateGenerationAsync(connectionId, operationId, encryptedEnvelope, cancellationToken);

            if (!await store.TryRecordStagedGenerationAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, nextName, operationId, cancellationToken))
            {
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "credential_stage_unknown");
            }

            if (!await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken))
            {
                return await RequireRecoveryAsync(claimed, tenantId, environmentId, "generation_publish_conflict");
            }

            var published = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return published == null
                ? new ConnectionLifecycleResult(false, "connection_unavailable", null)
                : new ConnectionLifecycleResult(true, null, published.Revision, connectionId, ToMetadata(published));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (providerCallStarted)
            {
                await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "refresh_outcome_unknown");
                throw new OperationCanceledException("Credential refresh was cancelled; provider outcome is unknown.", cancellationToken);
            }

            await TryReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "refresh_not_started");
            throw new OperationCanceledException("Credential refresh was cancelled before the provider call.", cancellationToken);
        }
        catch (Exception)
        {
            if (!providerCallStarted)
            {
                return await ReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "refresh_not_started");
            }

            // Provider/network/persistence errors after the durable call-start edge can hide a one-time refresh-token rotation.
            await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "refresh_outcome_unknown");
            return new ConnectionLifecycleResult(false, "refresh_outcome_unknown", expectedRevision);
        }
    }

    public async Task<ConnectionLifecycleResult> CleanupGenerationAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string connectionId,
        string generationId,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:cleanup", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        if (string.IsNullOrWhiteSpace(generationId))
        {
            return new ConnectionLifecycleResult(false, "generation_unavailable", null);
        }

        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (connection == null)
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        var cleanup = await store.FindGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, cancellationToken);
        if (cleanup?.Status == ConnectionGenerationCleanupStatus.Deleted)
        {
            return new ConnectionLifecycleResult(true, null, connection.Revision, connectionId, ToMetadata(connection));
        }

        var now = timeProvider.GetUtcNow();
        var cleanupClaim = await store.TryClaimGenerationCleanupAsync(
            connectionId, tenantId, environmentId, connection.Revision, generationId, now, now + OperationLeaseDuration, cancellationToken);
        if (cleanupClaim == null)
        {
            cleanup = await store.FindGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, cancellationToken);
            connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            if (cleanup?.Status == ConnectionGenerationCleanupStatus.Deleted && connection != null)
            {
                return new ConnectionLifecycleResult(true, null, connection.Revision, connectionId, ToMetadata(connection));
            }

            var errorCode = cleanup != null && cleanup.Status == ConnectionGenerationCleanupStatus.Deleting && cleanup.LeaseExpiresAt.HasValue && cleanup.LeaseExpiresAt.Value > timeProvider.GetUtcNow()
                ? "generation_cleanup_in_progress"
                : "generation_in_use";
            return new ConnectionLifecycleResult(false, errorCode, connection?.Revision, connectionId, connection is null ? null : ToMetadata(connection));
        }

        var name = ManagedSecretNames.ForGeneration(connectionId, generationId);
        try
        {
            // This host-only Secrets primitive validates the immutable owner/generation marker. The lifecycle
            // store claim above is the authorization and no-reference proof; raw host callers must not bypass it.
            if (!await secrets.DeleteGenerationAsync(name, connectionId, generationId, cancellationToken))
            {
                await store.CancelGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, cleanupClaim.Fence, CancellationToken.None);
                return new ConnectionLifecycleResult(false, "generation_unavailable", connection.Revision, connectionId, ToMetadata(connection));
            }

            if (!await store.CompleteGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, cleanupClaim.Fence, cancellationToken))
            {
                return new ConnectionLifecycleResult(false, "generation_cleanup_unknown", connection.Revision, connectionId, ToMetadata(connection));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Keep the durable Deleting tombstone. A retry repeats only the idempotent owner-checked deletion.
            throw new OperationCanceledException("Credential generation cleanup was cancelled; cleanup outcome is unknown.", cancellationToken);
        }
        catch (Exception)
        {
            // Keep the durable Deleting tombstone if the external Secrets write may have completed.
            return new ConnectionLifecycleResult(false, "generation_cleanup_unknown", connection.Revision, connectionId, ToMetadata(connection));
        }

        var updated = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        return updated == null
            ? new ConnectionLifecycleResult(false, "connection_unavailable", null)
            : new ConnectionLifecycleResult(true, null, updated.Revision, connectionId, ToMetadata(updated));
    }

    public async Task<ConnectionLifecycleResult> ReconcileAsync(string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(SystemPrincipal, ConnectionUseKind.BackgroundSystem, tenantId, environmentId, connectionId, "manage:reconcile", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (connection == null)
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        if (connection.OperationStatus is CredentialOperationStatus.None or CredentialOperationStatus.Completed)
        {
            return new ConnectionLifecycleResult(true, null, connection.Revision);
        }

        if (connection.OperationStatus == CredentialOperationStatus.Claimed)
        {
            var expiredClaim = await store.TryReleaseExpiredRefreshClaimAsync(
                connection.Id, tenantId, environmentId, connection.OperationId!, connection.OperationFence,
                timeProvider.GetUtcNow(), "refresh_not_started", cancellationToken);
            if (expiredClaim)
            {
                var released = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
                return new ConnectionLifecycleResult(false,
                    released?.Status == ConnectionStatus.Active ? "refresh_not_started" : "connection_unavailable",
                    released?.Revision, connectionId);
            }

            var latest = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return new ConnectionLifecycleResult(false,
                latest is { Status: ConnectionStatus.Active, OperationStatus: CredentialOperationStatus.Claimed or CredentialOperationStatus.ProviderCallStarted or CredentialOperationStatus.CredentialReceived }
                    ? "operation_in_progress"
                    : "connection_unavailable",
                latest?.Revision ?? connection.Revision, connectionId);
        }

        if (connection.OperationStatus is CredentialOperationStatus.ProviderCallStarted or CredentialOperationStatus.CredentialReceived)
        {
            var expired = await store.TryMarkRecoveryRequiredIfLeaseExpiredAsync(connection.Id, tenantId, environmentId, connection.OperationId!, connection.OperationFence, timeProvider.GetUtcNow(), "refresh_outcome_unknown", cancellationToken);
            if (expired)
            {
                return new ConnectionLifecycleResult(false, "refresh_outcome_unknown", connection.Revision + (connection.Status == ConnectionStatus.Active ? 1 : 0));
            }

            return new ConnectionLifecycleResult(false, "operation_in_progress", connection.Revision);
        }

        if (connection.OperationStatus == CredentialOperationStatus.Staged)
        {
            if (connection.OperationLeaseExpiresAt > timeProvider.GetUtcNow())
            {
                return new ConnectionLifecycleResult(false, "operation_in_progress", connection.Revision);
            }

            if (await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, connection.OperationExpectedRevision, connection.OperationId!, connection.OperationFence, cancellationToken))
            {
                return new ConnectionLifecycleResult(true, null, connection.OperationExpectedRevision + 1);
            }

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
                {
                    return new ConnectionLifecycleResult(true, null, connection.Revision + 1);
                }
            }
            catch (Exception)
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

    private static ConnectionLifecycleMetadata ToMetadata(IntegrationConnection connection) => new(
        connection.Id,
        connection.ProviderId,
        connection.ProviderAccountId,
        connection.Status,
        connection.Revision,
        connection.CurrentGenerationId);

    private IDisposable PushTenant(string tenantId) => tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

    private async Task<ConnectionLifecycleResult> ReleaseUnstartedRefreshAsync(IntegrationConnection connection, string tenantId, string environmentId, string safeErrorCode)
    {
        await TryReleaseUnstartedRefreshAsync(connection, tenantId, environmentId, safeErrorCode);
        var latest = await store.FindAsync(connection.Id, tenantId, environmentId, CancellationToken.None);
        return latest == null
            ? new ConnectionLifecycleResult(false, "connection_unavailable", null)
            : new ConnectionLifecycleResult(false, latest.Status == ConnectionStatus.Active ? safeErrorCode : "connection_unavailable", latest.Revision, connection.Id, ToMetadata(latest));
    }

    private async Task TryReleaseUnstartedRefreshAsync(IntegrationConnection connection, string tenantId, string environmentId, string safeErrorCode)
    {
        try
        {
            await store.TryReleaseUnstartedRefreshAsync(connection.Id, tenantId, environmentId,
                connection.OperationId!, connection.OperationFence, safeErrorCode, CancellationToken.None);
        }
        catch (Exception)
        {
            // The active credential remains the only published generation; reconciliation can release this claim after its lease.
        }
    }

    private static string Serialize(CredentialMaterial material) => JsonSerializer.Serialize(
        new CredentialEnvelope(material.AccessToken, material.RefreshToken, material.AccessTokenExpiresAt), JsonOptions);

    private static CredentialMaterial? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

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
