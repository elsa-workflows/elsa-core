using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Resolves adapter-specific structural validity without invoking provider test endpoints.
/// </summary>
public sealed class IdentityProviderConnectionValidityAssessor(
    IExternalAuthenticationAdapterRegistry adapters,
    IAdapterSettingsMigrationService settingsMigrations,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    ISystemClock clock) : IIdentityProviderConnectionValidityAssessor
{
    private readonly IReadOnlyDictionary<string, ISecretBindingResolver> _secretBindingResolvers =
        secretBindingResolvers.ToDictionary(x => x.Type, StringComparer.Ordinal);

    public async ValueTask<EffectiveIdentityProviderConnection> AssessAsync(
        EffectiveIdentityProviderConnection effective,
        CancellationToken cancellationToken = default)
    {
        if (effective.Validity == ConnectionValidity.Invalid)
            return effective;

        try
        {
            return await AssessCoreAsync(effective, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // A broken adapter or secret backend must not prevent every other sign-in method from being discovered.
            return effective with { Validity = ConnectionValidity.Invalid };
        }
    }

    private async ValueTask<EffectiveIdentityProviderConnection> AssessCoreAsync(
        EffectiveIdentityProviderConnection effective,
        CancellationToken cancellationToken)
    {

        var connection = IdentityProviderConnectionCloner.Clone(effective.Connection);
        if (!adapters.TryGet(connection.AdapterType, out var adapter))
            return effective with { Validity = ConnectionValidity.Invalid };

        try
        {
            var migration = await settingsMigrations.MigrateAsync(
                connection.AdapterType,
                connection.AdapterSettingsVersion,
                connection.AdapterSettings,
                cancellationToken);
            connection.AdapterSettingsVersion = migration.SettingsVersion;
            connection.AdapterSettings = migration.Settings;
        }
        catch (InvalidOperationException)
        {
            return effective with { Validity = ConnectionValidity.Invalid };
        }

        var descriptor = adapter.Describe();
        var declaredSecrets = descriptor.Fields
            .Where(x => x.IsSecretBinding)
            .ToDictionary(x => x.Name, StringComparer.Ordinal);
        if (connection.SecretBindings.Keys.Any(x => !declaredSecrets.ContainsKey(x)))
            return effective with { Validity = ConnectionValidity.Invalid };

        var secretStates = await GetSecretStatesAsync(connection, cancellationToken);
        if (declaredSecrets.Values
            .Where(x => x.IsRequired)
            .Any(field => !secretStates.TryGetValue(field.Name, out var state) || !state.IsConfigured || !state.IsResolvable))
            return effective with { Validity = ConnectionValidity.Invalid };

        var validation = await adapter.ValidateAsync(new(
            effective with { Connection = connection, Validity = ConnectionValidity.Unknown },
            new Dictionary<string, ResolvedSecretBinding>(),
            clock), cancellationToken);
        return effective with { Validity = validation.IsValid ? ConnectionValidity.Valid : ConnectionValidity.Invalid };
    }

    private async ValueTask<IReadOnlyDictionary<string, SecretBindingState>> GetSecretStatesAsync(
        IdentityProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var states = new Dictionary<string, SecretBindingState>(StringComparer.Ordinal);
        foreach (var (name, binding) in connection.SecretBindings)
        {
            states[name] = _secretBindingResolvers.TryGetValue(binding.ResolverType, out var resolver)
                ? await resolver.GetStateAsync(binding, cancellationToken)
                : new(false, false);
        }

        return states;
    }
}
