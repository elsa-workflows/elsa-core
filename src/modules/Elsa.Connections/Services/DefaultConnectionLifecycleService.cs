using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
    ITenantAccessor tenantAccessor,
    IConnectionOffboardingProvider? offboardingProvider = null) : IConnectionLifecycleService, IStaticApiKeyLifecycleService, IConnectionBackgroundUseService, IConnectionLifecycleRecoveryService
{
    private static readonly TimeSpan OperationLeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ClaimsPrincipal SystemPrincipal = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "elsa-connections-lifecycle"), new Claim("elsa:identity-kind", "system")],
        "Elsa.Connections.Server"));

    public async Task<ConnectionLifecycleResult> ConnectAsync(ClaimsPrincipal principal, ConnectConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.EnvironmentId) ||
            string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ProviderAccountId) ||
            string.IsNullOrWhiteSpace(request.InitialCredentials.AccessToken) || string.IsNullOrWhiteSpace(request.InitialCredentials.RefreshToken) ||
            request.InitialCredentials.AccessTokenExpiresAt <= timeProvider.GetUtcNow())
        {
            return new ConnectionLifecycleResult(false, "connection_input_invalid", null);
        }

        return await ConnectCoreAsync(principal, request.TenantId, request.EnvironmentId, request.ProviderId,
            request.ProviderAccountId, Serialize(request.InitialCredentials), cancellationToken);
    }

    public async Task<ConnectionLifecycleResult> ConnectApiKeyAsync(ClaimsPrincipal principal, ConnectApiKeyConnectionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.EnvironmentId) ||
            string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ProviderAccountId) ||
            string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return new ConnectionLifecycleResult(false, "connection_input_invalid", null);
        }

        return await ConnectCoreAsync(principal, request.TenantId, request.EnvironmentId, request.ProviderId,
            request.ProviderAccountId, SerializeApiKey(request.ApiKey), cancellationToken);
    }

    private async Task<ConnectionLifecycleResult> ConnectCoreAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string providerId,
        string providerAccountId,
        string encryptedEnvelope,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, "", "manage:connect", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        using var tenantContext = PushTenant(tenantId);
        var connectionId = Guid.NewGuid().ToString("N");
        var operationId = Guid.NewGuid().ToString("N");
        var secretName = ManagedSecretNames.ForGeneration(connectionId, operationId);
        var connection = new IntegrationConnection
        {
            Id = connectionId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ProviderId = providerId,
            ProviderAccountId = providerAccountId,
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
            await secrets.CreateGenerationAsync(connectionId, operationId, encryptedEnvelope, cancellationToken);
            if (!await store.TryRecordStagedGenerationAsync(connectionId, tenantId, environmentId, 1, operationId, 1, secretName, operationId, cancellationToken) ||
                !await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, 1, operationId, 1, cancellationToken))
            {
                await TryMarkRecoveryRequiredAsync(connection, tenantId, environmentId, "connection_publish_conflict");
                return new ConnectionLifecycleResult(false, "connection_publish_conflict", 1, connectionId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryMarkRecoveryRequiredAsync(connection, tenantId, environmentId, "connection_outcome_unknown");
            throw new OperationCanceledException("Connection setup was cancelled; creation outcome is unknown.", cancellationToken);
        }
        catch (Exception)
        {
            await TryMarkRecoveryRequiredAsync(connection, tenantId, environmentId, "connection_outcome_unknown");
            return new ConnectionLifecycleResult(false, "connection_outcome_unknown", 1, connectionId);
        }

        connection.CurrentSecretName = secretName;
        connection.CurrentGenerationId = operationId;
        connection.OperationStatus = CredentialOperationStatus.Completed;
        connection.Revision = 2;
        return new ConnectionLifecycleResult(true, null, connection.Revision, connectionId, ToMetadata(connection));
    }

    public async Task<ConnectionLifecycleResult> ReplaceApiKeyAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string connectionId,
        long expectedRevision,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || expectedRevision <= 0 ||
            !await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:rotate", cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", null);
        }

        using var tenantContext = PushTenant(tenantId);
        var current = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (current is not { Status: ConnectionStatus.Active } || current.Revision != expectedRevision ||
            !await IsApiKeyGenerationAsync(current, cancellationToken))
        {
            return new ConnectionLifecycleResult(false, "connection_unavailable", current?.Revision, connectionId,
                current == null ? null : ToMetadata(current));
        }

        var operationId = Guid.NewGuid().ToString("N");
        var claimed = await store.TryClaimCredentialUpdateAsync(connectionId, tenantId, environmentId,
            expectedRevision, operationId, timeProvider.GetUtcNow() + OperationLeaseDuration, cancellationToken);
        if (claimed == null)
        {
            var latest = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return new ConnectionLifecycleResult(false, "connection_conflict", latest?.Revision, connectionId,
                latest == null ? null : ToMetadata(latest));
        }

        var expectedOperationRevision = claimed.OperationExpectedRevision;
        var fence = claimed.OperationFence;
        var secretName = ManagedSecretNames.ForGeneration(connectionId, operationId);
        try
        {
            if (!await store.TryAcceptCredentialUpdateAsync(connectionId, tenantId, environmentId,
                    expectedOperationRevision, operationId, fence, timeProvider.GetUtcNow(), cancellationToken))
            {
                await TryReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "rotation_conflict");
                return new ConnectionLifecycleResult(false, "rotation_conflict", expectedOperationRevision, connectionId);
            }

            await secrets.CreateGenerationAsync(connectionId, operationId, SerializeApiKey(apiKey), cancellationToken);
            if (!await store.TryRecordStagedGenerationAsync(connectionId, tenantId, environmentId, expectedOperationRevision,
                    operationId, fence, secretName, operationId, cancellationToken) ||
                !await store.TryPublishGenerationAsync(connectionId, tenantId, environmentId, expectedOperationRevision,
                    operationId, fence, cancellationToken))
            {
                await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "rotation_publish_conflict");
                return new ConnectionLifecycleResult(false, "rotation_publish_conflict", expectedOperationRevision, connectionId);
            }

            var published = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return published == null
                ? new ConnectionLifecycleResult(false, "connection_unavailable", null)
                : new ConnectionLifecycleResult(true, null, published.Revision, connectionId, ToMetadata(published));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "rotation_outcome_unknown");
            throw new OperationCanceledException("API-key replacement outcome is unknown.", cancellationToken);
        }
        catch (Exception)
        {
            await TryMarkRecoveryRequiredAsync(claimed, tenantId, environmentId, "rotation_outcome_unknown");
            return new ConnectionLifecycleResult(false, "rotation_outcome_unknown", expectedOperationRevision, connectionId);
        }
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

        CredentialEnvelope? material;
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

        if (material == null || string.IsNullOrWhiteSpace(material.AccessToken) ||
            (material.Kind ?? ConnectionCredentialKind.OAuth) == ConnectionCredentialKind.OAuth &&
            (!material.AccessTokenExpiresAt.HasValue || material.AccessTokenExpiresAt <= timeProvider.GetUtcNow()))
        {
            throw new ConnectionUnavailableException();
        }

        var latest = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (latest == null || latest.Status != ConnectionStatus.Active ||
            latest.Revision != connection.Revision || latest.CurrentGenerationId != connection.CurrentGenerationId ||
            latest.CurrentSecretName != connection.CurrentSecretName ||
            latest.OperationStatus is not (CredentialOperationStatus.None or CredentialOperationStatus.Completed))
        {
            throw new ConnectionUnavailableException();
        }

        return new ConnectionAccessCredential(material.Kind ?? ConnectionCredentialKind.OAuth, material.AccessToken, material.AccessTokenExpiresAt);
    }

    public async Task<ConnectionOffboardingOperationResult> DisconnectAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:disconnect", cancellationToken))
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (connection == null)
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        var operationId = GetOffboardingOperationId(tenantId, environmentId, connectionId, ConnectionOffboardingOperationKind.LocalDisconnect, null);
        var existing = await store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        if (existing != null)
        {
            return new ConnectionOffboardingOperationResult(true, null, operationId, existing.Status, connection.Revision);
        }

        var now = timeProvider.GetUtcNow();
        var operation = new ConnectionOffboardingOperation
        {
            Id = operationId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ConnectionId = connectionId,
            ProviderId = connection.ProviderId,
            ProviderAccountId = connection.ProviderAccountId,
            Kind = ConnectionOffboardingOperationKind.LocalDisconnect,
            Status = ConnectionOffboardingOperationStatus.Completed,
            Fence = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        var disconnected = await store.TryDisconnectAndRecordAsync(connectionId, tenantId, environmentId, connection.Revision, operation, cancellationToken);
        if (disconnected != null)
        {
            return new ConnectionOffboardingOperationResult(true, null, operationId, operation.Status, disconnected.Revision);
        }

        var latestOperation = await store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        var latestConnection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        return latestOperation != null && latestConnection != null
            ? new ConnectionOffboardingOperationResult(true, null, operationId, latestOperation.Status, latestConnection.Revision)
            : new ConnectionOffboardingOperationResult(false, "connection_conflict", operationId, null, latestConnection?.Revision);
    }

    public async Task<ConnectionOffboardingOperationResult> RequestTokenRevocationAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string connectionId,
        string generationId,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:revoke", cancellationToken))
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        return await QueueOffboardingOperationAsync(tenantId, environmentId, connectionId,
            ConnectionOffboardingOperationKind.TokenPairRevocation, generationId, cancellationToken);
    }

    public async Task<ConnectionOffboardingOperationResult> RequestInstallationUninstallAsync(
        ClaimsPrincipal principal,
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(principal, ConnectionUseKind.Human, tenantId, environmentId, connectionId, "manage:uninstall", cancellationToken))
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        return await QueueOffboardingOperationAsync(tenantId, environmentId, connectionId,
            ConnectionOffboardingOperationKind.InstallationUninstall, null, cancellationToken);
    }

    public async Task<ConnectionOffboardingOperationResult> ReconcileOffboardingAsync(
        string tenantId,
        string environmentId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!await AuthorizeAsync(SystemPrincipal, ConnectionUseKind.BackgroundSystem, tenantId, environmentId, connectionId, "manage:reconcile", cancellationToken))
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        using var tenantContext = PushTenant(tenantId);
        var now = timeProvider.GetUtcNow();
        var stableRevocationIdempotency = offboardingProvider?.SupportsStableOperationIdIdempotency(ConnectionOffboardingOperationKind.TokenPairRevocation) == true;
        var stableUninstallIdempotency = offboardingProvider?.SupportsStableOperationIdIdempotency(ConnectionOffboardingOperationKind.InstallationUninstall) == true;
        var pending = await store.FindNextOffboardingOperationAsync(
            tenantId, environmentId, connectionId, now, stableRevocationIdempotency, stableUninstallIdempotency, cancellationToken);
        if (pending == null)
        {
            var current = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
            return new ConnectionOffboardingOperationResult(true, null, null, null, current?.Revision);
        }

        var supportsStableIdempotency = offboardingProvider?.SupportsStableOperationIdIdempotency(pending.Kind) == true;

        if (!supportsStableIdempotency && pending.Status == ConnectionOffboardingOperationStatus.UnknownOutcome)
        {
            return await GetOffboardingResultAsync(pending.Id, tenantId, environmentId, connectionId, false,
                "offboarding_outcome_unknown", cancellationToken);
        }

        if (!supportsStableIdempotency && pending.Status == ConnectionOffboardingOperationStatus.ProviderCallStarted &&
            pending.LeaseExpiresAt <= now)
        {
            await store.TryMarkOffboardingOutcomeUnknownIfLeaseExpiredAsync(pending.Id, tenantId, environmentId, connectionId,
                pending.Fence, now, "provider_outcome_unknown", CancellationToken.None);
            return await GetOffboardingResultAsync(pending.Id, tenantId, environmentId, connectionId, false,
                "offboarding_outcome_unknown", cancellationToken);
        }

        var claimed = await store.TryClaimOffboardingOperationAsync(
            pending.Id, tenantId, environmentId, connectionId, pending.Fence, now, now + OperationLeaseDuration, cancellationToken);
        if (claimed == null)
        {
            return await GetOffboardingResultAsync(pending.Id, tenantId, environmentId, connectionId, false, "offboarding_conflict", cancellationToken);
        }

        if (offboardingProvider == null)
        {
            await ReleaseOffboardingClaimAsync(claimed, tenantId, environmentId, connectionId, "offboarding_provider_unavailable");
            return await GetOffboardingResultAsync(claimed.Id, tenantId, environmentId, connectionId, false, "offboarding_provider_unavailable", cancellationToken);
        }

        CredentialMaterial? credentials = null;
        if (claimed.Kind == ConnectionOffboardingOperationKind.TokenPairRevocation)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(claimed.GenerationId))
                {
                    throw new ConnectionUnavailableException();
                }

                var secretName = ManagedSecretNames.ForGeneration(connectionId, claimed.GenerationId);
                var payload = await secrets.ResolveGenerationAsync(secretName, connectionId, claimed.GenerationId, cancellationToken);
                var envelope = Deserialize(payload.Value);
                if (envelope is not { Kind: null or ConnectionCredentialKind.OAuth, AccessToken: not null, RefreshToken: not null, AccessTokenExpiresAt: not null })
                {
                    throw new ConnectionUnavailableException();
                }

                credentials = new CredentialMaterial(envelope.AccessToken, envelope.RefreshToken, envelope.AccessTokenExpiresAt.Value);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await ReleaseOffboardingClaimAsync(claimed, tenantId, environmentId, connectionId, "offboarding_cancelled_before_call");
                throw new OperationCanceledException("Credential offboarding was cancelled before the provider call.", cancellationToken);
            }
            catch (Exception)
            {
                await ReleaseOffboardingClaimAsync(claimed, tenantId, environmentId, connectionId, "credential_unavailable");
                return await GetOffboardingResultAsync(claimed.Id, tenantId, environmentId, connectionId, false, "credential_unavailable", cancellationToken);
            }
        }

        now = timeProvider.GetUtcNow();
        if (!await store.TryStartOffboardingProviderCallAsync(claimed.Id, tenantId, environmentId, connectionId, claimed.Fence, now, cancellationToken))
        {
            await ReleaseOffboardingClaimAsync(claimed, tenantId, environmentId, connectionId, "offboarding_conflict");
            return await GetOffboardingResultAsync(claimed.Id, tenantId, environmentId, connectionId, false, "offboarding_conflict", cancellationToken);
        }

        try
        {
            var providerResult = claimed.Kind switch
            {
                ConnectionOffboardingOperationKind.TokenPairRevocation when credentials != null =>
                    await offboardingProvider.RevokeTokenPairAsync(claimed.ProviderId, claimed.ProviderAccountId, claimed.Id, credentials, cancellationToken),
                ConnectionOffboardingOperationKind.InstallationUninstall =>
                    await offboardingProvider.UninstallInstallationAsync(claimed.ProviderId, claimed.ProviderAccountId, claimed.Id, cancellationToken),
                _ => ConnectionOffboardingProviderResult.TerminalFailure
            };

            now = timeProvider.GetUtcNow();
            switch (providerResult)
            {
                case ConnectionOffboardingProviderResult.Succeeded:
                    // Once the provider has confirmed success, caller cancellation must not turn that known
                    // result into an unknown operation. Persist the semantic outcome independently.
                    await store.TryCompleteOffboardingOperationAsync(claimed.Id, tenantId, environmentId, connectionId, claimed.Fence, now, CancellationToken.None);
                    break;
                case ConnectionOffboardingProviderResult.RetryableFailure:
                    await RecordOffboardingFailureAsync(claimed, tenantId, environmentId, connectionId,
                        ConnectionOffboardingOperationStatus.RetryScheduled, "provider_retryable_failure");
                    break;
                case ConnectionOffboardingProviderResult.TerminalFailure:
                    await RecordOffboardingFailureAsync(claimed, tenantId, environmentId, connectionId,
                        ConnectionOffboardingOperationStatus.TerminalFailure, "provider_terminal_failure");
                    break;
                case ConnectionOffboardingProviderResult.UnknownOutcome:
                    await RecordOffboardingFailureAsync(claimed, tenantId, environmentId, connectionId,
                        ConnectionOffboardingOperationStatus.UnknownOutcome, "provider_outcome_unknown", supportsStableIdempotency);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RecordOffboardingFailureAsync(claimed, tenantId, environmentId, connectionId,
                ConnectionOffboardingOperationStatus.UnknownOutcome, "provider_outcome_unknown", supportsStableIdempotency);
            throw new OperationCanceledException("Credential offboarding was cancelled; provider outcome is unknown.", cancellationToken);
        }
        catch (Exception)
        {
            await RecordOffboardingFailureAsync(claimed, tenantId, environmentId, connectionId,
                ConnectionOffboardingOperationStatus.UnknownOutcome, "provider_outcome_unknown", supportsStableIdempotency);
        }

        return await GetOffboardingResultAsync(claimed.Id, tenantId, environmentId, connectionId, true, null, cancellationToken);
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
        var claimed = await store.TryClaimCredentialUpdateAsync(connectionId, tenantId, environmentId, current!.Revision, operationId, timeProvider.GetUtcNow() + OperationLeaseDuration, cancellationToken);
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
            if (oldMaterial?.Kind == ConnectionCredentialKind.ApiKey)
            {
                return await ReleaseUnstartedRefreshAsync(claimed, tenantId, environmentId, "credential_refresh_unsupported");
            }

            if (oldMaterial == null || oldMaterial.Kind is not null and not ConnectionCredentialKind.OAuth ||
                string.IsNullOrWhiteSpace(oldMaterial.AccessToken) || string.IsNullOrWhiteSpace(oldMaterial.RefreshToken) || !oldMaterial.AccessTokenExpiresAt.HasValue)
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

    private async Task<ConnectionOffboardingOperationResult> QueueOffboardingOperationAsync(
        string tenantId,
        string environmentId,
        string connectionId,
        ConnectionOffboardingOperationKind kind,
        string? generationId,
        CancellationToken cancellationToken)
    {
        if (kind == ConnectionOffboardingOperationKind.TokenPairRevocation && string.IsNullOrWhiteSpace(generationId) ||
            kind == ConnectionOffboardingOperationKind.InstallationUninstall && generationId != null)
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, null);
        }

        using var tenantContext = PushTenant(tenantId);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        if (connection is not { Status: ConnectionStatus.Disconnected } ||
            connection.OperationStatus is not (CredentialOperationStatus.None or CredentialOperationStatus.Completed))
        {
            return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, connection?.Revision);
        }

        var operationId = GetOffboardingOperationId(tenantId, environmentId, connectionId, kind, generationId);
        var existing = await store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        if (existing != null)
        {
            return new ConnectionOffboardingOperationResult(true, null, operationId, existing.Status, connection.Revision);
        }

        if (kind == ConnectionOffboardingOperationKind.TokenPairRevocation)
        {
            try
            {
                var payload = await secrets.ResolveGenerationAsync(ManagedSecretNames.ForGeneration(connectionId, generationId!), connectionId, generationId!, cancellationToken);
                if (Deserialize(payload.Value) is not { Kind: null or ConnectionCredentialKind.OAuth })
                {
                    throw new ConnectionUnavailableException();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return new ConnectionOffboardingOperationResult(false, "connection_unavailable", null, null, connection.Revision);
            }
        }

        var now = timeProvider.GetUtcNow();
        var operation = new ConnectionOffboardingOperation
        {
            Id = operationId,
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ConnectionId = connectionId,
            ProviderId = connection.ProviderId,
            ProviderAccountId = connection.ProviderAccountId,
            Kind = kind,
            GenerationId = generationId,
            Status = ConnectionOffboardingOperationStatus.Pending,
            Fence = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var queued = await store.TryQueueOffboardingOperationAsync(connection.Revision, operation, cancellationToken);
        if (queued != null)
        {
            return new ConnectionOffboardingOperationResult(true, null, operationId, queued.Status, connection.Revision + 1);
        }

        var latestOperation = await store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        var latestConnection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        return latestOperation != null && latestConnection != null
            ? new ConnectionOffboardingOperationResult(true, null, operationId, latestOperation.Status, latestConnection.Revision)
            : new ConnectionOffboardingOperationResult(false, "connection_conflict", operationId, null, latestConnection?.Revision);
    }

    private async Task<ConnectionOffboardingOperationResult> GetOffboardingResultAsync(
        string operationId,
        string tenantId,
        string environmentId,
        string connectionId,
        bool accepted,
        string? safeErrorCode,
        CancellationToken cancellationToken)
    {
        var operation = await store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        var connection = await store.FindAsync(connectionId, tenantId, environmentId, cancellationToken);
        return new ConnectionOffboardingOperationResult(accepted, safeErrorCode, operation?.Id, operation?.Status, connection?.Revision);
    }

    private async Task ReleaseOffboardingClaimAsync(
        ConnectionOffboardingOperation operation,
        string tenantId,
        string environmentId,
        string connectionId,
        string safeErrorCode)
    {
        var now = timeProvider.GetUtcNow();
        try
        {
            await store.TryReleaseOffboardingClaimAsync(operation.Id, tenantId, environmentId, connectionId, operation.Fence,
                now, now + TimeSpan.FromSeconds(30), safeErrorCode, CancellationToken.None);
        }
        catch (Exception)
        {
            // An expired claim is safe to retry; the provider-call state is never advanced here.
        }
    }

    private async Task RecordOffboardingFailureAsync(
        ConnectionOffboardingOperation operation,
        string tenantId,
        string environmentId,
        string connectionId,
        ConnectionOffboardingOperationStatus status,
        string safeErrorCode,
        bool supportsStableIdempotency = true)
    {
        var now = timeProvider.GetUtcNow();
        DateTimeOffset? nextAttemptAt = status == ConnectionOffboardingOperationStatus.RetryScheduled ||
                            status == ConnectionOffboardingOperationStatus.UnknownOutcome && supportsStableIdempotency
            ? now + TimeSpan.FromSeconds(30)
            : null;
        try
        {
            await store.TryRecordOffboardingFailureAsync(operation.Id, tenantId, environmentId, connectionId, operation.Fence,
                status, now, nextAttemptAt, safeErrorCode, CancellationToken.None);
        }
        catch (Exception)
        {
            // The durable provider-call lease expires and reconciliation retries with the same idempotency key.
        }
    }

    private static string GetOffboardingOperationId(
        string tenantId,
        string environmentId,
        string connectionId,
        ConnectionOffboardingOperationKind kind,
        string? generationId)
    {
        var canonicalIdentity = JsonSerializer.SerializeToUtf8Bytes(new string?[]
        {
            tenantId,
            environmentId,
            connectionId,
            kind.ToString(),
            generationId
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(canonicalIdentity)).ToLowerInvariant();
    }

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
        new CredentialEnvelope(ConnectionCredentialKind.OAuth, material.AccessToken, material.RefreshToken, material.AccessTokenExpiresAt), JsonOptions);

    private static string SerializeApiKey(string apiKey) => JsonSerializer.Serialize(
        new CredentialEnvelope(ConnectionCredentialKind.ApiKey, apiKey, null, null), JsonOptions);

    private static CredentialEnvelope? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CredentialEnvelope>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<bool> IsApiKeyGenerationAsync(IntegrationConnection connection, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connection.CurrentSecretName) || string.IsNullOrWhiteSpace(connection.CurrentGenerationId))
        {
            return false;
        }

        try
        {
            var payload = await secrets.ResolveGenerationAsync(connection.CurrentSecretName, connection.Id, connection.CurrentGenerationId, cancellationToken);
            return Deserialize(payload.Value)?.Kind == ConnectionCredentialKind.ApiKey;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
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

    private sealed record CredentialEnvelope(ConnectionCredentialKind? Kind, string? AccessToken, string? RefreshToken, DateTimeOffset? AccessTokenExpiresAt);
}
