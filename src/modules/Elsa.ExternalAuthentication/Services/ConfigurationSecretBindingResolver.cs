using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Microsoft.Extensions.Configuration;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Resolves a deployment-owned secret from configuration without ever exposing it through connection models.</summary>
public sealed class ConfigurationSecretBindingResolver(
    IConfiguration configuration,
    IExternalAuthenticationHandleHasher hasher) : ISecretBindingResolver
{
    public const string ResolverType = "configuration";
    public string Type => ResolverType;

    public ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureType(binding);
        var configured = !string.IsNullOrWhiteSpace(configuration[binding.Reference]);
        return ValueTask.FromResult(new SecretBindingState(configured, configured));
    }

    public ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureType(binding);
        var value = configuration[binding.Reference];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("The configured secret binding could not be resolved.");
        return ValueTask.FromResult(new ResolvedSecretBinding(new SensitiveString(value), hasher.Hash($"{ResolverType}:{binding.Reference}:{value}")));
    }

    private static void EnsureType(SecretBinding binding)
    {
        if (!string.Equals(binding.ResolverType, ResolverType, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(binding.Reference))
            throw new InvalidOperationException("The configured secret binding is invalid.");
    }
}
