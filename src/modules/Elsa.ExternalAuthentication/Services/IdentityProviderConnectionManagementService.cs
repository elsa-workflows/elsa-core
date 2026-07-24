using System.Security.Claims;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Providers;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Applies management-only invariants before mutating database-owned connections.
/// Connection stores remain responsible for durable compare-and-swap and unique-key enforcement.
/// </summary>
public sealed partial class IdentityProviderConnectionManagementService(
    IIdentityProviderConnectionStore store,
    IIdentityProviderConnectionRegistry registry,
    IConnectionRegistryVersionStore registryVersions,
    IExternalAuthenticationAdapterRegistry adapters,
    IAdapterSettingsMigrationService settingsMigrations,
    IUnlinkedIdentityPolicyRegistry policies,
    IPermissionGrantSourceRegistry grantSources,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IPermissionDelegationAuthorizer delegationAuthorizer,
    ConnectionRevisionCalculator revisionCalculator,
    ISystemClock clock,
    IOptions<ExternalAuthenticationOptions> options,
    IServiceProvider services)
{
    private readonly IReadOnlyDictionary<string, ISecretBindingResolver> _secretBindingResolvers = secretBindingResolvers.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<ManagementConnectionLookupResult> FindAsync(string id, string targetTenantId, CancellationToken cancellationToken = default)
    {
        var effective = await registry.FindByIdAsync(targetTenantId, id, cancellationToken);
        if (effective is not null)
            return new ManagementConnectionLookupResult.Found(await AssessValidityAsync(effective, cancellationToken));

        var connection = await store.FindByIdAsync(id, cancellationToken);
        if (connection is null || !IsApplicableToTenant(connection.TenantId, targetTenantId))
            return new ManagementConnectionLookupResult.NotFound();

        return new ManagementConnectionLookupResult.Found(await AssessValidityAsync(ToEffective(connection), cancellationToken));
    }

    public async ValueTask<IReadOnlyCollection<EffectiveIdentityProviderConnection>> ListAsync(string targetTenantId, ConnectionFilter filter, CancellationToken cancellationToken = default)
    {
        var effective = await registry.GetAsync(targetTenantId, cancellationToken);
        var matches = effective.Connections
            .Where(x => Matches(x, filter))
            .OrderBy(x => x.Scope.Kind)
            .ThenBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => x.Connection.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();
        return await Task.WhenAll(matches.Select(x => AssessValidityAsync(x, cancellationToken).AsTask()));
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
        if (existing is null || !IsApplicableToTenant(existing.TenantId, targetTenantId))
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
        var requireUnsafeConfirmation = adapters.TryGet(candidate.AdapterType, out var candidateAdapter) &&
            UnsafeSettingsChanged(existing.AdapterSettings, candidate.AdapterSettings, candidateAdapter.Describe());
        var validation = await ValidateAsync(candidate, actor, targetTenantId, requireCompleteConfiguration: candidate.IsEnabled, confirmUnsafeSettings, requireUnsafeConfirmation, allowIncompleteDraft: !candidate.IsEnabled, cancellationToken: cancellationToken);
        if (!validation.IsValid)
            return new ManagementConnectionMutationResult.ValidationFailed(validation);

        var keyOrScopeChanged = !string.Equals(existing.Key, candidate.Key, StringComparison.Ordinal) || !string.Equals(existing.TenantId, candidate.TenantId, StringComparison.Ordinal);
        if (keyOrScopeChanged && await CollidesWithConfigurationOrHostAsync(candidate, existing.Id, targetTenantId, cancellationToken))
            return new ManagementConnectionMutationResult.Conflict("connection_key_conflict");
        if (await IsBlockedByFinalLoginPathGuardAsync(existing, candidate, targetTenantId, actor, confirmFinalLoginPathOverride, cancellationToken))
            return new ManagementConnectionMutationResult.Conflict("final_login_path_guard");

        candidate.MaterialRevision = revisionCalculator.CalculateMaterialRevision(candidate);
        var result = await store.UpdateAsync(candidate, expectedRevision, cancellationToken);
        return await ProcessMutationAsync(result, actor, "updated", GetLifecycle(existing), cancellationToken, existing);
    }

    public async ValueTask<ManagementConnectionMutationResult> ChangeLifecycleAsync(string id, ConnectionLifecycle action, long expectedRevision, ClaimsPrincipal actor, string targetTenantId, bool confirmFinalLoginPathOverride = false, CancellationToken cancellationToken = default)
    {
        var existing = await store.FindByIdAsync(id, cancellationToken);
        if (existing is null || !IsApplicableToTenant(existing.TenantId, targetTenantId))
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
                if (candidate.IsDefault)
                {
                    var current = await registry.GetAsync(targetTenantId, cancellationToken);
                    if (current.Connections.Any(x => x.Ownership == ConnectionSourceOwnership.Database && x.Connection.IsDefault && !x.Connection.ArchivedAt.HasValue && x.Scope == ToScope(candidate.TenantId) && !string.Equals(x.Connection.Id, candidate.Id, StringComparison.Ordinal)))
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
        return await ProcessMutationAsync(result, actor, action.ToString().ToLowerInvariant(), previousLifecycle, cancellationToken, existing);
    }

    public async ValueTask<ConnectionValidationResult> ValidateAsync(IdentityProviderConnection connection, ClaimsPrincipal actor, string targetTenantId, bool requireCompleteConfiguration, bool confirmUnsafeSettings, bool requireUnsafeConfirmation = false, bool allowIncompleteDraft = false, CancellationToken cancellationToken = default)
    {
        var errors = new List<ConnectionValidationError>();
        var warnings = new List<string>();
        var configuredOptions = options.Value;
        ValidateEnvelope(connection, configuredOptions, errors);

        if (!adapters.TryGet(connection.AdapterType, out var adapter) || !IsAllowed(configuredOptions.AllowedAdapterTypes, connection.AdapterType))
            errors.Add(new ConnectionValidationError("adapterType", "unavailable", "The selected adapter is not installed or is not allowed by this deployment."));

        ValidatePolicy(connection, configuredOptions, errors);
        ValidateGrantSources(connection, configuredOptions, errors);
        if (connection.PermissionGrantSources.Count != 0)
        {
            var delegation = await delegationAuthorizer.AuthorizeAsync(actor, connection.PermissionGrantSources.ToArray(), cancellationToken);
            if (!delegation.IsAuthorized)
                errors.Add(new ConnectionValidationError("permissionGrantSources", "delegation_denied", "The caller may not delegate one or more configured permissions."));
        }

        if (adapter is null)
            return new ConnectionValidationResult(false, errors, warnings);

        await ApplySettingsMigrationAsync(connection, errors, cancellationToken);
        if (errors.Count != 0)
            return new ConnectionValidationResult(false, errors, warnings);

        var descriptor = adapter.Describe();
        ValidateSecretBindingFields(connection, descriptor, requireCompleteConfiguration, errors);
        ValidateSecretBindingsAreNotAdapterSettings(connection.AdapterSettings, descriptor, errors);
        if (requireUnsafeConfirmation && UsesUnsafeSettings(connection.AdapterSettings, descriptor) && (!confirmUnsafeSettings || !HasPermission(actor, ExternalAuthenticationPermissions.ProviderTrustUnsafe)))
            errors.Add(new ConnectionValidationError("adapterSettings", "unsafe_confirmation_required", "Unsafe provider trust settings require permission and explicit confirmation."));

        if (requireCompleteConfiguration)
        {
            var secretStates = await GetSecretStatesAsync(connection, cancellationToken);
            foreach (var (name, state) in secretStates)
            {
                if (!state.IsConfigured)
                    errors.Add(new ConnectionValidationError($"secretBindings.{name}", "required", "A required secret binding is not configured."));
                else if (!state.IsResolvable)
                    errors.Add(new ConnectionValidationError($"secretBindings.{name}", "unresolvable", "The secret binding cannot be resolved."));
            }
        }

        if (errors.Count != 0 || allowIncompleteDraft)
            return new ConnectionValidationResult(errors.Count == 0, errors, warnings);

        var effective = ToEffective(connection);
        var adapterValidation = await adapter.ValidateAsync(new ConnectionValidationContext(effective, new Dictionary<string, ResolvedSecretBinding>(), clock), cancellationToken);
        errors.AddRange(adapterValidation.Errors);
        warnings.AddRange(adapterValidation.Warnings);
        return new ConnectionValidationResult(errors.Count == 0 && adapterValidation.IsValid, errors, warnings);
    }

    public ValueTask<IReadOnlyDictionary<string, SecretBindingState>> GetSecretBindingStatesAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default) => GetSecretStatesAsync(connection, cancellationToken);

    private async ValueTask<bool> IsBlockedByFinalLoginPathGuardAsync(IdentityProviderConnection existing, IdentityProviderConnection candidate, string targetTenantId, ClaimsPrincipal actor, bool confirmedOverride, CancellationToken cancellationToken)
    {
        var guard = services.GetService<FinalLoginPathGuard>();
        return guard is not null && await guard.AuthorizeAsync(existing, candidate, targetTenantId, actor, confirmedOverride, cancellationToken) == FinalLoginPathGuardResult.Denied;
    }

    private async ValueTask<ManagementConnectionMutationResult> ProcessMutationAsync(ConnectionMutationResult result, ClaimsPrincipal actor, string operation, ConnectionLifecycle? previousLifecycle, CancellationToken cancellationToken, IdentityProviderConnection? previousConnection = null)
    {
        switch (result)
        {
            case ConnectionMutationResult.Created(var createdConnection):
                await registryVersions.AdvanceAsync(cancellationToken);
                await PublishAsync(createdConnection, actor, operation, previousLifecycle, previousConnection, cancellationToken);
                return new ManagementConnectionMutationResult.Success(createdConnection);
            case ConnectionMutationResult.Updated(var updatedConnection):
                await registryVersions.AdvanceAsync(cancellationToken);
                await PublishAsync(updatedConnection, actor, operation, previousLifecycle, previousConnection, cancellationToken);
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
            return true;

        if (candidate.TenantId != ConnectionScope.HostTenantId)
            return effective.Connections.Any(x =>
            !string.Equals(x.Connection.Id, selfId, StringComparison.Ordinal) &&
            x.Scope.Kind == ConnectionScopeKind.Host &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), key, StringComparison.Ordinal));

        var rows = await store.FindAsync(new ConnectionFilter(), cancellationToken);
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
                states[name] = new SecretBindingState(false, false);
            else
                states[name] = await resolver.GetStateAsync(binding, cancellationToken);
        }

        return states;
    }

    // Registry composition deliberately leaves adapter-specific structural validity as Unknown.
    // Management reads resolve that state without invoking provider test endpoints or persisting migrations.
    private async ValueTask<EffectiveIdentityProviderConnection> AssessValidityAsync(EffectiveIdentityProviderConnection effective, CancellationToken cancellationToken)
    {
        if (effective.Validity == ConnectionValidity.Invalid)
            return effective;

        var connection = IdentityProviderConnectionCloner.Clone(effective.Connection);
        if (!adapters.TryGet(connection.AdapterType, out var adapter))
            return effective with { Validity = ConnectionValidity.Invalid };

        try
        {
            var migration = await settingsMigrations.MigrateAsync(connection.AdapterType, connection.AdapterSettingsVersion, connection.AdapterSettings, cancellationToken);
            connection.AdapterSettingsVersion = migration.SettingsVersion;
            connection.AdapterSettings = migration.Settings;
        }
        catch (InvalidOperationException)
        {
            return effective with { Validity = ConnectionValidity.Invalid };
        }

        var descriptor = adapter.Describe();
        var declaredSecrets = descriptor.Fields.Where(x => x.IsSecretBinding).ToDictionary(x => x.Name, StringComparer.Ordinal);
        if (connection.SecretBindings.Keys.Any(x => !declaredSecrets.ContainsKey(x)))
            return effective with { Validity = ConnectionValidity.Invalid };

        var states = await GetSecretStatesAsync(connection, cancellationToken);
        if (declaredSecrets.Values.Where(x => x.IsRequired).Any(field => !states.TryGetValue(field.Name, out var state) || !state.IsConfigured || !state.IsResolvable))
            return effective with { Validity = ConnectionValidity.Invalid };

        var validation = await adapter.ValidateAsync(new ConnectionValidationContext(
            new EffectiveIdentityProviderConnection(connection, effective.Ownership, effective.Scope, ConnectionValidity.Unknown, effective.IsShadowed, effective.SourceName),
            new Dictionary<string, ResolvedSecretBinding>(),
            clock), cancellationToken);
        return effective with { Validity = validation.IsValid ? ConnectionValidity.Valid : ConnectionValidity.Invalid };
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
            errors.Add(new ConnectionValidationError("adapterSettingsVersion", "migration_unavailable", "The adapter settings version is not compatible with the installed adapter."));
        }
    }

    private void NormalizeForCreate(IdentityProviderConnection connection, string targetTenantId)
    {
        connection.Id = string.IsNullOrWhiteSpace(connection.Id) ? Guid.NewGuid().ToString("N") : connection.Id;
        connection.Key = connection.Key?.Trim() ?? string.Empty;
        connection.TenantId = NormalizeScopeTenantId(connection.TenantId, targetTenantId);
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
        candidate.TenantId = NormalizeScopeTenantId(candidate.TenantId, existing.TenantId);
        candidate.UpdatedAt = clock.UtcNow;
        candidate.SecretBindings ??= new Dictionary<string, SecretBinding>(StringComparer.Ordinal);
        candidate.PermissionGrantSources ??= [];
        candidate.ClaimProjection ??= ClaimProjection.Empty;
    }

    private static void ValidateEnvelope(IdentityProviderConnection connection, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors)
    {
        if (!configuredOptions.EnableDatabaseConnections)
            errors.Add(new ConnectionValidationError("source", "disabled", "Database-owned connections are disabled by deployment configuration."));
        if (string.IsNullOrWhiteSpace(connection.Key) || connection.Key.Length > 128 || connection.Key.Any(char.IsWhiteSpace))
            errors.Add(new ConnectionValidationError("key", "invalid", "Connection keys must be non-empty lowercase URL-safe tokens up to 128 characters."));
        else if (!ConnectionKeyPattern().IsMatch(connection.Key))
            errors.Add(new ConnectionValidationError("key", "invalid", "Connection keys must use lowercase letters, digits, and interior hyphens only."));
        if (string.IsNullOrWhiteSpace(connection.DisplayName) || connection.DisplayName.Trim().Length > 256)
            errors.Add(new ConnectionValidationError("displayName", "invalid", "Display name is required and may not exceed 256 characters."));
        if (connection.AdapterSettingsVersion <= 0)
            errors.Add(new ConnectionValidationError("adapterSettingsVersion", "invalid", "Adapter settings version must be positive."));
        if (!Enum.IsDefined(connection.UpstreamLogoutMode))
            errors.Add(new ConnectionValidationError("upstreamLogoutMode", "invalid", "Upstream logout mode is invalid."));
        if (!IsValidScope(connection.TenantId))
            errors.Add(new ConnectionValidationError("scope", "invalid", "The connection scope is invalid."));
        if (connection.ClaimProjection.MaximumClaimCount < 0 || connection.ClaimProjection.MaximumValueLength < 0 || connection.ClaimProjection.MaximumTotalBytes < 0 ||
            connection.ClaimProjection.MaximumClaimCount > configuredOptions.Claims.MaximumClaimCount || connection.ClaimProjection.MaximumValueLength > configuredOptions.Claims.MaximumValueLength || connection.ClaimProjection.MaximumTotalBytes > configuredOptions.Claims.MaximumTotalBytes)
            errors.Add(new ConnectionValidationError("claimProjection", "invalid", "Claim projection limits exceed deployment bounds."));
        if (!connection.ClaimProjection.RedactedClaimTypes.IsSubsetOf(connection.ClaimProjection.AllowedClaimTypes))
            errors.Add(new ConnectionValidationError("claimProjection.redactedClaimTypes", "invalid", "Redacted claim types must also be allowed claim types."));
    }

    private void ValidatePolicy(IdentityProviderConnection connection, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors)
    {
        if (connection.UnlinkedPolicy is not { } policy)
            return;
        if (!configuredOptions.UnlinkedIdentityPolicy.AllowDatabaseConnectionOverride)
            errors.Add(new ConnectionValidationError("unlinkedPolicy", "not_allowed", "This deployment does not allow database connection policy overrides."));
        else if (policy.SettingsVersion <= 0 || !policies.TryGet(policy.Type, out _) || !IsAllowed(configuredOptions.AllowedUnlinkedIdentityPolicyTypes, policy.Type))
            errors.Add(new ConnectionValidationError("unlinkedPolicy", "unavailable", "The selected unlinked identity policy is not installed or allowed."));
    }

    private void ValidateGrantSources(IdentityProviderConnection connection, ExternalAuthenticationOptions configuredOptions, ICollection<ConnectionValidationError> errors)
    {
        var orders = new HashSet<int>();
        foreach (var source in connection.PermissionGrantSources)
        {
            if (source.SettingsVersion <= 0 || !grantSources.TryGet(source.Type, out _) || !IsAllowed(configuredOptions.AllowedPermissionGrantSourceTypes, source.Type))
                errors.Add(new ConnectionValidationError("permissionGrantSources", "unavailable", "A selected permission grant source is not installed or allowed."));
            if (!orders.Add(source.Order))
                errors.Add(new ConnectionValidationError("permissionGrantSources", "duplicate_order", "Permission grant source orders must be unique."));
        }
    }

    private static void ValidateSecretBindingFields(IdentityProviderConnection connection, ExternalAuthenticationAdapterDescriptor descriptor, bool requireCompleteConfiguration, ICollection<ConnectionValidationError> errors)
    {
        var secretFields = descriptor.Fields.Where(x => x.IsSecretBinding).ToDictionary(x => x.Name, StringComparer.Ordinal);
        foreach (var name in connection.SecretBindings.Keys)
            if (!secretFields.ContainsKey(name))
                errors.Add(new ConnectionValidationError($"secretBindings.{name}", "undeclared", "The adapter does not declare this secret binding field."));
        if (!requireCompleteConfiguration)
            return;
        foreach (var field in secretFields.Values.Where(x => x.IsRequired))
            if (!connection.SecretBindings.ContainsKey(field.Name))
                errors.Add(new ConnectionValidationError($"secretBindings.{field.Name}", "required", "A required secret binding is missing."));
    }

    private static void ValidateSecretBindingsAreNotAdapterSettings(JsonElement settings, ExternalAuthenticationAdapterDescriptor descriptor, ICollection<ConnectionValidationError> errors)
    {
        if (settings.ValueKind != JsonValueKind.Object)
            return;

        foreach (var field in descriptor.Fields.Where(x => x.IsSecretBinding))
            if (settings.TryGetProperty(field.Name, out _))
                errors.Add(new ConnectionValidationError($"adapterSettings.{field.Name}", "secret_binding_required", "Secret fields must be configured through Secret Bindings, not adapter settings."));
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
    private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new ConnectionScope(ConnectionScopeKind.Tenant, tenantId);
    private static bool IsApplicableToTenant(string connectionTenantId, string targetTenantId) => connectionTenantId == ConnectionScope.HostTenantId || string.Equals(connectionTenantId, targetTenantId, StringComparison.Ordinal);
    private static bool CanMutate(string connectionTenantId, string targetTenantId) => string.Equals(connectionTenantId, targetTenantId, StringComparison.Ordinal);
    private static bool IsValidScope(string tenantId) => tenantId == ConnectionScope.HostTenantId || tenantId.Length == 0 || !string.IsNullOrWhiteSpace(tenantId);
    private static string NormalizeScopeTenantId(string requestedTenantId, string fallback) => requestedTenantId is null ? fallback : requestedTenantId.Trim();
    private static bool IsAllowed(ICollection<string> allowedTypes, string type) => allowedTypes.Count == 0 || allowedTypes.Contains(type, StringComparer.Ordinal);
    private static bool HasPermission(ClaimsPrincipal actor, string permission) => actor.FindAll(PermissionNames.ClaimType).Any(x => x.Value == PermissionNames.All || x.Value == permission);
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
