using System.Security.Claims;
using System.Text.Json;
using Elsa.Common;
using Elsa.Authorization;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Providers;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Applies management-only invariants before mutating database-owned connections.
/// Connection stores remain responsible for durable compare-and-swap and unique-key enforcement.
/// </summary>
public sealed partial class IdentityProviderConnectionManagementService(
    IIdentityProviderConnectionStore store,
    IIdentityProviderConnectionRegistry registry,
    IIdentityProviderConnectionValidityAssessor validityAssessor,
    IConnectionRegistryVersionStore registryVersions,
    IExternalAuthenticationAdapterRegistry adapters,
    IAdapterSettingsMigrationService settingsMigrations,
    IUnlinkedIdentityPolicyRegistry policies,
    IExternalUserMatcherRegistry matchers,
    IPermissionGrantSourceRegistry grantSources,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IEnumerable<IManagedSecretBindingWriter> managedSecretBindingWriters,
    IPermissionDelegationAuthorizer delegationAuthorizer,
    IPermissionEvaluator permissionEvaluator,
    ConnectionRevisionCalculator revisionCalculator,
    ISystemClock clock,
    IOptions<ExternalAuthenticationOptions> options,
    Elsa.Identity.Contracts.IRoleAuthorizationService roleAuthorizationService,
    IExternalAuthenticationSessionStore sessions,
    IServiceProvider services,
    ILogger<IdentityProviderConnectionManagementService> logger)
{
    private readonly IReadOnlyDictionary<string, ISecretBindingResolver> _secretBindingResolvers = secretBindingResolvers.ToDictionary(x => x.Type, StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, IManagedSecretBindingWriter> _managedSecretBindingWriters = managedSecretBindingWriters.ToDictionary(x => x.ResolverType, StringComparer.Ordinal);

    public async ValueTask<ManagementConnectionLookupResult> FindAsync(string id, string targetTenantId, CancellationToken cancellationToken = default)
    {
        var effective = await registry.FindByIdAsync(targetTenantId, id, cancellationToken);
        if (effective is not null && effective.Scope == ConnectionScope.Host)
            return new ManagementConnectionLookupResult.Found(await validityAssessor.AssessAsync(effective, cancellationToken));

        var connection = await store.FindByIdAsync(id, cancellationToken);
        if (connection is null || connection.TenantId != ConnectionScope.HostTenantId)
            return new ManagementConnectionLookupResult.NotFound();

        return new ManagementConnectionLookupResult.Found(await validityAssessor.AssessAsync(ToEffective(connection), cancellationToken));
    }

    /// <summary>Returns the deployment-derived read-only upstream callback URI for management display.</summary>
    public Uri? GetProviderCallbackUri(IdentityProviderConnection connection) => options.Value.Redirects.ExternalCallbackBaseUri is { } baseUri
        ? ExternalAuthenticationCallbackUris.GetAuthorizationCallbackUri(baseUri, connection, BrokerTransactionPurpose.ExternalSignIn)
        : null;

    /// <summary>Returns the deployment-derived read-only callback URI used by provider preview sign-ins.</summary>
    public Uri? GetProviderPreviewCallbackUri(IdentityProviderConnection connection) => options.Value.Redirects.ExternalCallbackBaseUri is { } baseUri
        ? ExternalAuthenticationCallbackUris.GetAuthorizationCallbackUri(baseUri, connection, BrokerTransactionPurpose.Preview)
        : null;

    public async ValueTask<IReadOnlyCollection<EffectiveIdentityProviderConnection>> ListAsync(string targetTenantId, ConnectionFilter filter, CancellationToken cancellationToken = default)
    {
        var effective = await registry.GetAsync(targetTenantId, cancellationToken);
        var matches = effective.Connections
            .Where(x => x.Scope == ConnectionScope.Host)
            .Where(x => Matches(x, filter))
            .OrderBy(x => x.Scope.Kind)
            .ThenBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => x.Connection.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();
        return await Task.WhenAll(matches.Select(x => validityAssessor.AssessAsync(x, cancellationToken).AsTask()));
    }

    public async ValueTask<ManagementConnectionMutationResult> CreateAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string targetTenantId, bool confirmUnsafeSettings, CancellationToken cancellationToken = default)
    {
        NormalizeForCreate(connection, targetTenantId);
        if (!CanMutate(connection.TenantId, targetTenantId))
            return new ManagementConnectionMutationResult.Forbidden();
        var validation = await ValidateAsync(connection, actor, targetTenantId, requireCompleteConfiguration: false, confirmUnsafeSettings, requireUnsafeConfirmation: true, allowIncompleteDraft: true, cancellationToken: cancellationToken);
        if (!validation.IsValid)
            return new ManagementConnectionMutationResult.ValidationFailed(validation);
        if (await CollidesWithConfigurationOrHostAsync(connection, null, targetTenantId, cancellationToken))
            return new ManagementConnectionMutationResult.Conflict("connection_key_conflict");

        connection.MaterialRevision = revisionCalculator.CalculateMaterialRevision(connection);
        var result = await store.CreateAsync(connection, cancellationToken);
        return await ProcessMutationAsync(result, actor, "created", null, cancellationToken);
    }

    public async ValueTask<ManagementConnectionMutationResult> UpdateAsync(string id, IdentityProviderConnection candidate, long expectedRevision, ClaimsPrincipal actor, string targetTenantId, bool confirmUnsafeSettings, bool confirmFinalLoginPathOverride = false, CancellationToken cancellationToken = default)
    {
        var existing = await store.FindByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != ConnectionScope.HostTenantId)
            return new ManagementConnectionMutationResult.NotFound();
        if (!CanMutate(existing.TenantId, targetTenantId))
            return new ManagementConnectionMutationResult.Forbidden();

        candidate.Id = existing.Id;
        candidate.CreatedAt = existing.CreatedAt;
        candidate.Revision = existing.Revision;
        candidate.ArchivedAt = existing.ArchivedAt;
        NormalizeForUpdate(candidate, existing);
        if (!CanMutate(candidate.TenantId, targetTenantId))
            return new ManagementConnectionMutationResult.Forbidden();
        if (!string.Equals(existing.Key, candidate.Key, StringComparison.Ordinal))
            return new ManagementConnectionMutationResult.Conflict("connection_key_immutable");
        var requireUnsafeConfirmation = adapters.TryGet(candidate.AdapterType, out var candidateAdapter) &&
            UnsafeSettingsChanged(existing.AdapterSettings, candidate.AdapterSettings, candidateAdapter.Describe());
        var validation = await ValidateAsync(candidate, actor, targetTenantId, requireCompleteConfiguration: candidate.IsEnabled, confirmUnsafeSettings, requireUnsafeConfirmation, allowIncompleteDraft: !candidate.IsEnabled, cancellationToken: cancellationToken);
        if (!validation.IsValid)
            return new ManagementConnectionMutationResult.ValidationFailed(validation);

        if (await IsBlockedByFinalLoginPathGuardAsync(existing, candidate, targetTenantId, actor, confirmFinalLoginPathOverride, cancellationToken))
            return new ManagementConnectionMutationResult.Conflict("final_login_path_guard");

        candidate.MaterialRevision = revisionCalculator.CalculateMaterialRevision(candidate);
        var result = await store.UpdateAsync(candidate, expectedRevision, cancellationToken);
        return await ProcessMutationAsync(result, actor, "updated", GetLifecycle(existing), cancellationToken, existing);
    }

    public async ValueTask<ManagementConnectionMutationResult> ChangeLifecycleAsync(string id, ConnectionLifecycle action, long expectedRevision, ClaimsPrincipal actor, string targetTenantId, bool confirmFinalLoginPathOverride = false, bool revokeActiveSessions = false, CancellationToken cancellationToken = default)
    {
        var existing = await store.FindByIdAsync(id, cancellationToken);
        if (existing is null || existing.TenantId != ConnectionScope.HostTenantId)
            return new ManagementConnectionMutationResult.NotFound();
        if (!CanMutate(existing.TenantId, targetTenantId))
            return new ManagementConnectionMutationResult.Forbidden();

        var previousLifecycle = GetLifecycle(existing);
        var candidate = IdentityProviderConnectionCloner.Clone(existing);
        switch (action)
        {
            case ConnectionLifecycle.Enabled:
            {
                var validation = await ValidateAsync(candidate, actor, targetTenantId, requireCompleteConfiguration: true, confirmUnsafeSettings: false, requireUnsafeConfirmation: false, cancellationToken: cancellationToken);
                if (!validation.IsValid)
                    return new ManagementConnectionMutationResult.ValidationFailed(validation);
                if (candidate.ArchivedAt.HasValue)
                    return new ManagementConnectionMutationResult.Conflict("connection_archived");
                if (candidate.IsPreferred)
                {
                    var current = await registry.GetAsync(targetTenantId, cancellationToken);
                    if (current.Connections.Any(x =>
                            x is { Ownership: ConnectionSourceOwnership.Configuration, IsShadowed: false, Connection: { IsEnabled: true, ArchivedAt: null, IsPreferred: true } } &&
                            x.Validity != ConnectionValidity.Invalid &&
                            (!candidate.OverridesConfigurationConnection || !string.Equals(x.Connection.Key, candidate.Key, StringComparison.Ordinal))))
                        return new ManagementConnectionMutationResult.Conflict("configuration_preferred_connection");
                    if (current.Connections.Any(x => x is { Ownership: ConnectionSourceOwnership.Database, Connection: { IsPreferred: true, ArchivedAt: null } } && x.Scope == ToScope(candidate.TenantId) && !string.Equals(x.Connection.Id, candidate.Id, StringComparison.Ordinal)))
                        return new ManagementConnectionMutationResult.Conflict("default_connection_conflict");
                }
                candidate.IsEnabled = true;
                break;
            }
            case ConnectionLifecycle.Disabled:
                if (candidate.ArchivedAt.HasValue)
                    return new ManagementConnectionMutationResult.Conflict("connection_archived");
                candidate.IsEnabled = false;
                break;
            case ConnectionLifecycle.Archived:
                candidate.IsEnabled = false;
                candidate.ArchivedAt = clock.UtcNow;
                break;
            case ConnectionLifecycle.Draft:
                if (!candidate.ArchivedAt.HasValue)
                    return new ManagementConnectionMutationResult.Conflict("connection_not_archived");
                candidate.ArchivedAt = null;
                candidate.IsEnabled = false;
                break;
            default:
                return new ManagementConnectionMutationResult.Conflict("invalid_lifecycle_action");
        }

        candidate.UpdatedAt = clock.UtcNow;
        candidate.MaterialRevision = revisionCalculator.CalculateMaterialRevision(candidate);
        if (await IsBlockedByFinalLoginPathGuardAsync(existing, candidate, targetTenantId, actor, confirmFinalLoginPathOverride, cancellationToken))
            return new ManagementConnectionMutationResult.Conflict("final_login_path_guard");
        var result = await store.UpdateAsync(candidate, expectedRevision, cancellationToken);
        var processed = await ProcessMutationAsync(result, actor, action.ToString().ToLowerInvariant(), previousLifecycle, cancellationToken, existing);
        if (processed is ManagementConnectionMutationResult.Success && action == ConnectionLifecycle.Disabled && revokeActiveSessions)
        {
            var connectionKey = ConnectionRevisionCalculator.NormalizeKey(candidate.Key);
            var revokedCount = await sessions.RevokeActiveForConnectionAsync(connectionKey, "connection_disabled", clock.UtcNow, cancellationToken);
            await PublishBulkSessionRevocationAsync(candidate, actor, revokedCount);
        }
        return processed;
    }

    public async ValueTask<ConnectionValidationResult> ValidateAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string targetTenantId, bool requireCompleteConfiguration, bool confirmUnsafeSettings, bool requireUnsafeConfirmation = false, bool allowIncompleteDraft = false, CancellationToken cancellationToken = default)
    {
        var errors = new List<ConnectionValidationError>();
        var warnings = new List<string>();
        var configuredOptions = options.Value;
        ValidateEnvelope(connection, configuredOptions, errors);

        if (!adapters.TryGet(connection.AdapterType, out var adapter) || !IsAllowed(configuredOptions.AllowedAdapterTypes, connection.AdapterType))
            errors.Add(new("adapterType", "unavailable", "The selected adapter is not installed or is not allowed by this deployment."));

        await ValidatePolicyAsync(connection, actor, targetTenantId, configuredOptions, errors, cancellationToken);
        ValidateGrantSources(connection, configuredOptions, errors);
        if (connection.PermissionGrantSources.Count != 0)
        {
            var delegation = await delegationAuthorizer.AuthorizeAsync(actor, connection.PermissionGrantSources.ToArray(), cancellationToken);
            if (!delegation.IsAuthorized)
                errors.Add(new("permissionGrantSources", "delegation_denied", "The caller may not delegate one or more configured permissions."));
        }

        if (adapter is null)
            return new(false, errors, warnings);

        await ApplySettingsMigrationAsync(connection, errors, cancellationToken);
        if (errors.Count != 0)
            return new(false, errors, warnings);

        var descriptor = adapter.Describe();
        ValidateSecretBindingFields(connection, descriptor, requireCompleteConfiguration, errors);
        ValidateSecretBindingsAreNotAdapterSettings(connection.AdapterSettings, descriptor, errors);
        if (requireUnsafeConfirmation && UsesUnsafeSettings(connection.AdapterSettings, descriptor) && (!confirmUnsafeSettings || !permissionEvaluator.HasPermission(actor, ExternalAuthenticationResourcePermissions.ProviderTrust, ExternalAuthenticationVerbs.Override)))
            errors.Add(new("adapterSettings", "unsafe_confirmation_required", "Unsafe provider trust settings require permission and explicit confirmation."));

        if (requireCompleteConfiguration)
        {
            var secretStates = await GetSecretStatesAsync(connection, cancellationToken);
            foreach (var (name, state) in secretStates)
            {
                if (!state.IsConfigured)
                    errors.Add(new($"secretBindings.{name}", "required", "A required secret binding is not configured."));
                else if (!state.IsResolvable)
                    errors.Add(new($"secretBindings.{name}", "unresolvable", "The secret binding cannot be resolved."));
            }
        }

        if (errors.Count != 0 || allowIncompleteDraft)
            return new(errors.Count == 0, errors, warnings);

        var effective = ToEffective(connection);
        var adapterValidation = await adapter.ValidateAsync(new(effective, new Dictionary<string, ResolvedSecretBinding>(), clock), cancellationToken);
        errors.AddRange(adapterValidation.Errors);
        warnings.AddRange(adapterValidation.Warnings);
        return new(errors.Count == 0 && adapterValidation.IsValid, errors, warnings);
    }

    public ValueTask<IReadOnlyDictionary<string, SecretBindingState>> GetSecretBindingStatesAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default) => GetSecretStatesAsync(connection, cancellationToken);

    public SecretBindingPresentation PresentSecretBinding(SecretBinding binding, SecretBindingState? state) => new(
        binding.Ownership == SecretBindingOwnership.Managed ? "managed" : "external",
        state?.IsConfigured ?? false,
        state?.IsResolvable ?? false);

    public bool CanCreateConfigurationOverride() => options.Value.AllowConfigurationConnectionOverrides;

    public bool CanPromoteToConfigurationOverride(EffectiveIdentityProviderConnection connection) =>
        connection is { Ownership: ConnectionSourceOwnership.Database, IsShadowed: true, Connection.ArchivedAt: null } &&
        options.Value.AllowConfigurationConnectionOverrides;

    private async ValueTask<bool> IsBlockedByFinalLoginPathGuardAsync(IdentityProviderConnection existing, IdentityProviderConnection candidate, string targetTenantId, ClaimsPrincipal actor, bool confirmedOverride, CancellationToken cancellationToken)
    {
        var guard = services.GetService<FinalLoginPathGuard>();
        if (guard is null)
            return false;

        var guardExisting = existing;
        if (!existing.OverridesConfigurationConnection && candidate.OverridesConfigurationConnection)
        {
            var normalizedKey = ConnectionRevisionCalculator.NormalizeKey(candidate.Key);
            var effective = await registry.GetAsync(targetTenantId, cancellationToken);
            var displacedConfigurationConnection = effective.Connections.FirstOrDefault(x =>
                x is { Ownership: ConnectionSourceOwnership.Configuration, IsShadowed: false } &&
                string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), normalizedKey, StringComparison.Ordinal));
            if (displacedConfigurationConnection is not null)
                guardExisting = displacedConfigurationConnection.Connection;
        }

        return await guard.AuthorizeAsync(guardExisting, candidate, targetTenantId, actor, confirmedOverride, cancellationToken) == FinalLoginPathGuardResult.Denied;
    }

    private async ValueTask<ManagementConnectionMutationResult> ProcessMutationAsync(ConnectionMutationResult result, ClaimsPrincipal actor, string operation, ConnectionLifecycle? previousLifecycle, CancellationToken cancellationToken, IdentityProviderConnection? previousConnection = null)
    {
        switch (result)
        {
            case ConnectionMutationResult.Created(var createdConnection):
                await RunPostCommitActionsAsync(createdConnection, actor, operation, previousLifecycle, previousConnection);
                return new ManagementConnectionMutationResult.Success(createdConnection);
            case ConnectionMutationResult.Updated(var updatedConnection):
                await RunPostCommitActionsAsync(updatedConnection, actor, operation, previousLifecycle, previousConnection);
                return new ManagementConnectionMutationResult.Success(updatedConnection);
            case ConnectionMutationResult.NotFound:
                return new ManagementConnectionMutationResult.NotFound();
            case ConnectionMutationResult.DuplicateKey:
                return new ManagementConnectionMutationResult.Conflict("connection_key_conflict");
            case ConnectionMutationResult.RevisionConflict(var currentRevision):
                return new ManagementConnectionMutationResult.PreconditionFailed(currentRevision);
            default:
                throw new InvalidOperationException("The connection store returned an unknown mutation result.");
        }
    }

    private async ValueTask RunPostCommitActionsAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string operation, ConnectionLifecycle? previousLifecycle, IdentityProviderConnection? previousConnection)
    {
        try
        {
            await registryVersions.AdvanceAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Connection {ConnectionId} was committed, but advancing the external-authentication registry version failed.", connection.Id);
        }

        try
        {
            await PublishAsync(connection, actor, operation, previousLifecycle, previousConnection, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Connection {ConnectionId} was committed, but publishing external-authentication security notifications failed.", connection.Id);
        }
    }

    private async ValueTask PublishBulkSessionRevocationAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, int revokedCount)
    {
        if (revokedCount == 0)
            return;

        var notificationSender = services.GetService<INotificationSender>();
        if (notificationSender is null)
            return;

        try
        {
            var context = new SecurityEventContext(
                actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? actor.FindFirstValue("sub"),
                connection.TenantId,
                connection.Id,
                null,
                clock.UtcNow,
                SecurityEventOutcome.Succeeded,
                Guid.NewGuid().ToString("N"),
                "Active external authentication sessions were revoked when the connection was disabled.");
            await notificationSender.SendAsync(new ExternalAuthenticationConnectionSessionsRevoked(context, revokedCount, "connection_disabled"), CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Connection {ConnectionId} sessions were revoked, but publishing the aggregate security notification failed.", connection.Id);
        }
    }

    private async ValueTask PublishAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string operation, ConnectionLifecycle? previousLifecycle, IdentityProviderConnection? previousConnection, CancellationToken cancellationToken)
    {
        var notificationSender = services.GetService<INotificationSender>();
        if (notificationSender is null)
            return;

        var context = new SecurityEventContext(
            actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? actor.FindFirstValue("sub"),
            connection.TenantId,
            connection.Id,
            null,
            clock.UtcNow,
            SecurityEventOutcome.Succeeded,
            Guid.NewGuid().ToString("N"),
            "Identity provider connection management operation completed.");
        await notificationSender.SendAsync(new IdentityProviderConnectionChanged(context, operation, connection.Revision, connection.MaterialRevision), cancellationToken);
        if (previousLifecycle is { } previous && previous != GetLifecycle(connection))
            await notificationSender.SendAsync(new IdentityProviderConnectionLifecycleChanged(context, previous.ToString(), GetLifecycle(connection).ToString(), connection.Revision), cancellationToken);
        if (previousConnection is not null)
        {
            var fields = previousConnection.SecretBindings.Keys
                .Concat(connection.SecretBindings.Keys)
                .Distinct(StringComparer.Ordinal)
                .Where(field => !previousConnection.SecretBindings.TryGetValue(field, out var before) || !connection.SecretBindings.TryGetValue(field, out var after) || before != after);
            foreach (var field in fields)
            {
                previousConnection.SecretBindings.TryGetValue(field, out var previousBinding);
                connection.SecretBindings.TryGetValue(field, out var binding);
                var isConfigured = binding is not null && _secretBindingResolvers.TryGetValue(binding.ResolverType, out var resolver) && (await resolver.GetStateAsync(binding, cancellationToken)).IsConfigured;
                await notificationSender.SendAsync(new IdentityProviderConnectionSecretBindingChanged(context, field, binding?.ResolverType ?? previousBinding?.ResolverType ?? string.Empty, isConfigured), cancellationToken);
            }
        }
    }

    private async ValueTask<bool> CollidesWithConfigurationOrHostAsync(IdentityProviderConnection candidate, string? selfId, string targetTenantId, CancellationToken cancellationToken)
    {
        var lookupTenant = candidate.TenantId == ConnectionScope.HostTenantId ? ConnectionScope.HostTenantId : candidate.TenantId;
        var effective = await registry.GetAsync(lookupTenant, cancellationToken);
        var key = ConnectionRevisionCalculator.NormalizeKey(candidate.Key);
        if (effective.Connections.Any(x => x.Ownership == ConnectionSourceOwnership.Configuration && x.Scope.TenantId == candidate.TenantId && string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), key, StringComparison.Ordinal)))
            return !candidate.OverridesConfigurationConnection;

        if (candidate.TenantId != ConnectionScope.HostTenantId)
            return effective.Connections.Any(x =>
            !string.Equals(x.Connection.Id, selfId, StringComparison.Ordinal) &&
            x.Scope.Kind == ConnectionScopeKind.Host &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), key, StringComparison.Ordinal));

        var rows = await store.FindAsync(new(), cancellationToken);
        if (rows.Items.Any(x =>
                !string.Equals(x.Id, selfId, StringComparison.Ordinal) &&
                x.TenantId != ConnectionScope.HostTenantId &&
                string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Key), key, StringComparison.Ordinal)))
            return true;

        return options.Value.ConfigurationConnections.Any(x =>
            x.TenantId != ConnectionScope.HostTenantId &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Key), key, StringComparison.Ordinal));
    }

    private async ValueTask<IReadOnlyDictionary<string, SecretBindingState>> GetSecretStatesAsync(IdentityProviderConnection connection, CancellationToken cancellationToken)
    {
        var states = new Dictionary<string, SecretBindingState>(StringComparer.Ordinal);
        foreach (var (name, binding) in connection.SecretBindings)
        {
            if (!_secretBindingResolvers.TryGetValue(binding.ResolverType, out var resolver))
                states[name] = new(false, false);
            else
                states[name] = await resolver.GetStateAsync(binding, cancellationToken);
        }

        return states;
    }

    private async ValueTask ApplySettingsMigrationAsync(IdentityProviderConnection connection, ICollection<ConnectionValidationError> errors, CancellationToken cancellationToken)
    {
        try
        {
            var migration = await settingsMigrations.MigrateAsync(connection.AdapterType, connection.AdapterSettingsVersion, connection.AdapterSettings, cancellationToken);
            connection.AdapterSettingsVersion = migration.SettingsVersion;
            connection.AdapterSettings = migration.Settings;
        }
        catch (InvalidOperationException)
        {
            errors.Add(new("adapterSettingsVersion", "migration_unavailable", "The adapter settings version is not compatible with the installed adapter."));
        }
    }

    private void NormalizeForCreate(IdentityProviderConnection connection, string targetTenantId)
    {
        connection.Id = string.IsNullOrWhiteSpace(connection.Id) ? Guid.NewGuid().ToString("N") : connection.Id;
        connection.Key = connection.Key?.Trim() ?? string.Empty;
        connection.TenantId = ConnectionScope.HostTenantId;
        connection.IsEnabled = false;
        connection.ArchivedAt = null;
        connection.Revision = 1;
        connection.CreatedAt = clock.UtcNow;
        connection.UpdatedAt = clock.UtcNow;
        connection.MaterialRevision = revisionCalculator.CalculateMaterialRevision(connection);
    }

    private void NormalizeForUpdate(IdentityProviderConnection candidate, IdentityProviderConnection existing)
    {
        candidate.Key = candidate.Key?.Trim() ?? string.Empty;
        candidate.TenantId = ConnectionScope.HostTenantId;
        candidate.IsEnabled = existing.IsEnabled;
        candidate.UpdatedAt = clock.UtcNow;
        candidate.SecretBindings ??= new Dictionary<string, SecretBinding>(StringComparer.Ordinal);
        candidate.PermissionGrantSources ??= [];
        candidate.ClaimProjection ??= ClaimProjection.Empty;
    }

    private static void ValidateEnvelope(IdentityProviderConnection connection, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors)
    {
        if (!configuredOptions.EnableDatabaseConnections)
            errors.Add(new("source", "disabled", "Database-owned connections are disabled by deployment configuration."));
        if (connection.OverridesConfigurationConnection && !configuredOptions.AllowConfigurationConnectionOverrides)
            errors.Add(new("overridesConfigurationConnection", "not_allowed", "This deployment does not allow database connections to override configuration-owned connections."));
        if (string.IsNullOrWhiteSpace(connection.Key) || connection.Key.Length > 128 || connection.Key.Any(char.IsWhiteSpace))
            errors.Add(new("key", "invalid", "Connection keys must be non-empty lowercase URL-safe tokens up to 128 characters."));
        else if (!ConnectionKeyPattern().IsMatch(connection.Key))
            errors.Add(new("key", "invalid", "Connection keys must use lowercase letters, digits, and interior hyphens only."));
        if (string.IsNullOrWhiteSpace(connection.DisplayName) || connection.DisplayName.Trim().Length > 256)
            errors.Add(new("displayName", "invalid", "Display name is required and may not exceed 256 characters."));
        if (connection.AdapterSettingsVersion <= 0)
            errors.Add(new("adapterSettingsVersion", "invalid", "Adapter settings version must be positive."));
        if (!Enum.IsDefined(connection.UpstreamLogoutMode))
            errors.Add(new("upstreamLogoutMode", "invalid", "Upstream logout mode is invalid."));
        if (!IsValidScope(connection.TenantId))
            errors.Add(new("scope", "host_scope_required", "Identity provider connections are managed host-wide in this version."));
        if (connection.ClaimProjection.MaximumClaimCount < 0 || connection.ClaimProjection.MaximumValueLength < 0 || connection.ClaimProjection.MaximumTotalBytes < 0 ||
            connection.ClaimProjection.MaximumClaimCount > configuredOptions.Claims.MaximumClaimCount || connection.ClaimProjection.MaximumValueLength > configuredOptions.Claims.MaximumValueLength || connection.ClaimProjection.MaximumTotalBytes > configuredOptions.Claims.MaximumTotalBytes)
            errors.Add(new("claimProjection", "invalid", "Claim projection limits exceed deployment bounds."));
        if (!connection.ClaimProjection.RedactedClaimTypes.IsSubsetOf(connection.ClaimProjection.AllowedClaimTypes))
            errors.Add(new("claimProjection.redactedClaimTypes", "invalid", "Redacted claim types must also be allowed claim types."));
    }

    private async ValueTask ValidatePolicyAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string targetTenantId, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors, CancellationToken cancellationToken)
    {
        // The roles the candidate would actually assign. A policy that does not create users assigns none,
        // and so does no policy at all -- which is what makes switching away from, or clearing, a stored
        // create-user fallback a change rather than a no-op.
        var defaultRoleIds = connection.UnlinkedPolicy is { } candidate && UsesCreateUserFallback(candidate)
            ? Policies.CreateUserUnlinkedIdentityPolicy.ReadRoleIds(candidate.Settings)
            : [];

        // Two independent checks, reported separately because they answer different questions. The
        // permission asks whether this actor may decide what auto-created users receive; the subset rule
        // asks whether these particular roles stay within what the actor already holds. Only the second
        // existed, which left the sibling resource guarded on the write path while the roles inside it
        // were not -- see #7977.
        //
        // Gated on the effective set *changing*, not on it being non-empty, and evaluated before the
        // null-policy early return. Validation runs on every update, on enabling a connection, and on
        // read-only validate, so keying off presence would stop an administrator without this permission
        // from editing an unrelated field once anyone had set roles. Evaluating it only for non-null
        // policies would be worse: omitting unlinkedPolicy from an update clears a stored fallback and
        // drops its role assignments, the same decision as switching it to 'reject'.
        //
        // The cheap in-memory permission check goes first: the unchanged-roles comparison rebuilds the
        // effective registry, and its answer is irrelevant for an actor who holds the permission.
        if (!permissionEvaluator.HasPermission(actor, ExternalAuthenticationResourcePermissions.PolicyDefaultRoles, CoreVerbs.Update)
            && !await DefaultRolesAreUnchangedAsync(connection, targetTenantId, defaultRoleIds, cancellationToken))
            errors.Add(new("unlinkedPolicy.defaultRoleIds", "forbidden", "Changing the default roles for an unlinked identity policy requires the policy default roles update permission."));

        if (connection.UnlinkedPolicy is not { } policy)
            return;
        if (!configuredOptions.UnlinkedIdentityPolicy.AllowDatabaseConnectionOverride)
            errors.Add(new("unlinkedPolicy", "not_allowed", "This deployment does not allow database connection policy overrides."));
        else if (policy.SettingsVersion <= 0 || !policies.TryGet(policy.Type, out _) || !IsAllowed(configuredOptions.AllowedUnlinkedIdentityPolicyTypes, policy.Type))
            errors.Add(new("unlinkedPolicy", "unavailable", "The selected unlinked identity policy is not installed or allowed."));
        else
        {
            // The subset rule only has something to say about roles actually being assigned.
            if (UsesCreateUserFallback(policy) && !await roleAuthorizationService.CanAssignRolesAsync(actor, defaultRoleIds, cancellationToken))
                errors.Add(new("unlinkedPolicy.defaultRoleIds", "forbidden", "The selected default roles are unavailable or grant permissions the actor cannot delegate."));

            if (string.Equals(policy.Type, Policies.MatchExternalUserUnlinkedIdentityPolicy.PolicyType, StringComparison.Ordinal) &&
                (!TryGetMatcherSelection(policy.Settings, out var matcherType, out var matcherSettingsVersion) ||
                 !IsAllowed(configuredOptions.AllowedExternalUserMatcherTypes, matcherType) ||
                 !matchers.TryGet(matcherType, out _) ||
                 matchers.ListDescriptors().All(x => !string.Equals(x.Type, matcherType, StringComparison.Ordinal) || x.SettingsVersion != matcherSettingsVersion)))
                errors.Add(new("unlinkedPolicy.matcher", "unavailable", "The selected external user matcher is not installed or allowed."));
        }
    }

    /// <summary>Whether <paramref name="candidateRoleIds"/> matches what the connection already assigns.</summary>
    /// <remarks>
    /// The baseline comes from the registry rather than the database store, because a configuration-owned
    /// connection has no database record: looking only there made its configured roles read as newly assigned
    /// on every validation, so a caller with view access could not validate one at all. The registry answers
    /// for both ownerships, which is the question being asked -- what does this connection assign today.
    /// </remarks>
    private async ValueTask<bool> DefaultRolesAreUnchangedAsync(IdentityProviderConnection connection, string targetTenantId, IReadOnlyCollection<string> candidateRoleIds, CancellationToken cancellationToken)
    {
        var existing = string.IsNullOrWhiteSpace(connection.Id)
            ? null
            : (await registry.FindByIdAsync(targetTenantId, connection.Id, cancellationToken))?.Connection
              ?? await store.FindByIdAsync(connection.Id, cancellationToken);
        var storedRoleIds = existing?.UnlinkedPolicy is { } storedPolicy && UsesCreateUserFallback(storedPolicy)
            ? Policies.CreateUserUnlinkedIdentityPolicy.ReadRoleIds(storedPolicy.Settings)
            : [];

        // Order is not meaningful in a role set, so a reordering is not a change.
        return storedRoleIds.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(candidateRoleIds.OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static bool UsesCreateUserFallback(PolicySelection policy) =>
        string.Equals(policy.Type, Policies.CreateUserUnlinkedIdentityPolicy.PolicyType, StringComparison.Ordinal) ||
        string.Equals(policy.Type, Policies.MatchExternalUserUnlinkedIdentityPolicy.PolicyType, StringComparison.Ordinal) &&
        string.Equals(ReadString(policy.Settings, "noMatchAction"), "create-user", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetMatcherSelection(JsonElement settings, out string matcherType, out int settingsVersion)
    {
        matcherType = string.Empty;
        settingsVersion = 0;
        if (settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("matcher", out var matcher) || matcher.ValueKind != JsonValueKind.Object || !matcher.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
            return false;

        matcherType = type.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(matcherType) && matcher.TryGetProperty("settingsVersion", out var version) && version.TryGetInt32(out settingsVersion) && settingsVersion > 0;
    }

    private static string? ReadString(JsonElement settings, string propertyName) =>
        settings.ValueKind == JsonValueKind.Object &&
        settings.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private void ValidateGrantSources(IdentityProviderConnection connection, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors)
    {
        var orders = new HashSet<int>();
        foreach (var source in connection.PermissionGrantSources)
        {
            if (source.SettingsVersion <= 0 || !grantSources.TryGet(source.Type, out _) || !IsAllowed(configuredOptions.AllowedPermissionGrantSourceTypes, source.Type))
                errors.Add(new("permissionGrantSources", "unavailable", "A selected permission grant source is not installed or allowed."));
            if (!orders.Add(source.Order))
                errors.Add(new("permissionGrantSources", "duplicate_order", "Permission grant source orders must be unique."));
        }
    }

    private static void ValidateSecretBindingFields(IdentityProviderConnection connection, ExternalAuthenticationAdapterDescriptor descriptor, bool requireCompleteConfiguration, ICollection<ConnectionValidationError> errors)
    {
        var secretFields = descriptor.Fields.Where(x => x.IsSecretBinding).ToDictionary(x => x.Name, StringComparer.Ordinal);
        foreach (var name in connection.SecretBindings.Keys)
            if (!secretFields.ContainsKey(name))
                errors.Add(new($"secretBindings.{name}", "undeclared", "The adapter does not declare this secret binding field."));
        if (!requireCompleteConfiguration)
            return;
        foreach (var field in secretFields.Values.Where(x => x.IsRequired))
            if (!connection.SecretBindings.ContainsKey(field.Name))
                errors.Add(new($"secretBindings.{field.Name}", "required", "A required secret binding is missing."));
    }

    private static void ValidateSecretBindingsAreNotAdapterSettings(JsonElement settings, ExternalAuthenticationAdapterDescriptor descriptor, ICollection<ConnectionValidationError> errors)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return;

        foreach (var field in descriptor.Fields.Where(x => x.IsSecretBinding))
            if (settings.TryGetProperty(field.Name, out _))
                errors.Add(new($"adapterSettings.{field.Name}", "secret_binding_required", "Secret fields must be configured through Secret Bindings, not adapter settings."));
    }

    private static bool UsesUnsafeSettings(JsonElement settings, ExternalAuthenticationAdapterDescriptor descriptor)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return false;
        return descriptor.Fields.Where(x => x.IsUnsafe).Any(field =>
            settings.TryGetProperty(field.Name, out var value) && IsUnsafeFieldValue(field, value));
    }

    private static bool UnsafeSettingsChanged(JsonElement beforeSettings, JsonElement afterSettings, ExternalAuthenticationAdapterDescriptor descriptor)
    {
        if (afterSettings.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var field in descriptor.Fields.Where(x => x.IsUnsafe))
        {
            if (!afterSettings.TryGetProperty(field.Name, out var afterValue) || !IsUnsafeFieldValue(field, afterValue))
                continue;
            if (beforeSettings.ValueKind != JsonValueKind.Object || !beforeSettings.TryGetProperty(field.Name, out var beforeValue) || !JsonValueEquals(beforeValue, afterValue))
                return true;
        }

        return false;
    }

    private static bool IsUnsafeFieldValue(SettingFieldDescriptor field, JsonElement value) =>
        value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined and not JsonValueKind.False &&
        (!string.Equals(field.Name, "providerPkce", StringComparison.Ordinal) || value.ValueKind != JsonValueKind.String || string.Equals(value.GetString(), "disabled", StringComparison.OrdinalIgnoreCase));

    private static bool JsonValueEquals(JsonElement left, JsonElement right) =>
        left.ValueKind == right.ValueKind && string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);

    private static EffectiveIdentityProviderConnection ToEffective(IdentityProviderConnection connection) => new(connection, ConnectionSourceOwnership.Database, ToScope(connection.TenantId), ConnectionValidity.Unknown, false, DatabaseIdentityProviderConnectionSource.SourceName);
    private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new(ConnectionScopeKind.Tenant, tenantId);
    private static bool CanMutate(string connectionTenantId, string targetTenantId) => connectionTenantId == ConnectionScope.HostTenantId || string.Equals(connectionTenantId, targetTenantId, StringComparison.Ordinal);
    private static bool IsValidScope(string tenantId) => tenantId == ConnectionScope.HostTenantId;
    private static string NormalizeScopeTenantId(string requestedTenantId, string fallback) => requestedTenantId is null ? fallback : requestedTenantId.Trim();
    private static bool IsAllowed(ICollection<string> allowedTypes, string type) => allowedTypes.Count == 0 || allowedTypes.Contains(type, StringComparer.Ordinal);
    private static ConnectionLifecycle GetLifecycle(IdentityProviderConnection connection) => connection.ArchivedAt.HasValue ? ConnectionLifecycle.Archived : connection.IsEnabled ? ConnectionLifecycle.Enabled : ConnectionLifecycle.Disabled;
    private static bool Matches(EffectiveIdentityProviderConnection connection, ConnectionFilter filter) =>
        (filter.Ownership is null || filter.Ownership == connection.Ownership) &&
        (filter.Scope is null || filter.Scope == connection.Scope) &&
        (string.IsNullOrWhiteSpace(filter.Search) || connection.Connection.Key.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) || connection.Connection.DisplayName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(filter.AdapterType) || string.Equals(filter.AdapterType, connection.Connection.AdapterType, StringComparison.Ordinal)) &&
        (!filter.IsEnabled.HasValue || filter.IsEnabled.Value == connection.Connection.IsEnabled) &&
        (!filter.IsArchived.HasValue || filter.IsArchived.Value == connection.Connection.ArchivedAt.HasValue);

    [System.Text.RegularExpressions.GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,126}[a-z0-9])?$")]
    private static partial System.Text.RegularExpressions.Regex ConnectionKeyPattern();
}

public abstract record ManagementConnectionLookupResult
{
    private ManagementConnectionLookupResult() { }
    public sealed record Found(EffectiveIdentityProviderConnection Connection) : ManagementConnectionLookupResult;
    public sealed record NotFound : ManagementConnectionLookupResult;
}

public abstract record ManagementConnectionMutationResult
{
    private ManagementConnectionMutationResult() { }
    public sealed record Success(IdentityProviderConnection Connection) : ManagementConnectionMutationResult;
    public sealed record NotFound : ManagementConnectionMutationResult;
    public sealed record Conflict(string Code) : ManagementConnectionMutationResult;
    public sealed record PreconditionFailed(long CurrentRevision) : ManagementConnectionMutationResult;
    public sealed record Forbidden : ManagementConnectionMutationResult;
    public sealed record ValidationFailed(ConnectionValidationResult Validation) : ManagementConnectionMutationResult;
}
