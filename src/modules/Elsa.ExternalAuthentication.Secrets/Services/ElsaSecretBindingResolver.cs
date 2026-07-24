using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.ExternalAuthentication.Secrets.Services;

/// <summary>
/// Resolves External Authentication secret references through Elsa Secrets
/// without exposing secret values or generation metadata to management models.
/// </summary>
public sealed class ElsaSecretBindingResolver(
    ISecretManager secretManager,
    IExternalAuthenticationHandleHasher handleHasher) : ISecretBindingResolver
{
    public const string ResolverType = "elsa-secrets";
    public string Type => ResolverType;

    public async ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        EnsureResolverType(binding);
        var secret = await secretManager.GetAsync(binding.Reference, cancellationToken);
        if (secret is null)
            return new SecretBindingState(false, false);

        var configured = secret.Status == SecretStatus.Active && secret.LatestActiveVersion is not null;
        if (!configured || !IsCompatible(secret, binding))
            return new SecretBindingState(configured, false);

        var test = await secretManager.TestAsync(secret.Name, cancellationToken);
        return new SecretBindingState(true, test.Succeeded);
    }

    public async ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        EnsureResolverType(binding);
        var secret = await secretManager.GetAsync(binding.Reference, cancellationToken)
            ?? throw new InvalidOperationException("The configured secret binding could not be resolved.");
        if (!IsCompatible(secret, binding))
            throw new InvalidOperationException("The configured secret binding is incompatible with the required type or scope.");
        if (secret.Status != SecretStatus.Active || secret.LatestActiveVersion is not { } version)
            throw new InvalidOperationException("The configured secret binding is not active.");

        var payload = await secretManager.ResolvePayloadAsync(secret, cancellationToken);
        if (payload.Value is null)
            throw new InvalidOperationException("The configured secret binding could not be resolved.");

        var fingerprint = handleHasher.Hash($"{ResolverType}:{secret.Id}:{version.Version}:{version.CreatedAt.ToUnixTimeMilliseconds()}");
        return new ResolvedSecretBinding(new SensitiveString(payload.Value), fingerprint);
    }

    private static void EnsureResolverType(SecretBinding binding)
    {
        if (!string.Equals(binding.ResolverType, ResolverType, StringComparison.Ordinal))
            throw new InvalidOperationException("The secret binding selects a different resolver type.");
        if (string.IsNullOrWhiteSpace(binding.Reference))
            throw new InvalidOperationException("The secret binding reference is required.");
    }

    private static bool IsCompatible(Secret secret, SecretBinding binding) =>
        (string.IsNullOrWhiteSpace(binding.ExpectedType) || string.Equals(secret.TypeName, binding.ExpectedType, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(binding.ExpectedScope) || string.Equals(secret.Scope, binding.ExpectedScope, StringComparison.OrdinalIgnoreCase));
}
