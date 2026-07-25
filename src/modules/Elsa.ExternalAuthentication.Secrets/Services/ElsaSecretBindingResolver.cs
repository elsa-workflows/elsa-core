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
    IExternalAuthenticationHandleHasher handleHasher) : ISecretBindingResolver, IManagedSecretBindingWriter
{
    public const string ResolverType = "elsa-secrets";
    public string Type => ResolverType;
    string IManagedSecretBindingWriter.ResolverType => ResolverType;
    string IManagedSecretBindingWriter.DisplayName => "Elsa Secrets";

    public async ValueTask<SecretBinding> StageAsync(ManagedSecretBindingWriteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConnectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FieldName);
        var fieldName = string.Concat(request.FieldName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        if (fieldName.Length == 0)
            throw new ArgumentException("The secret field name must contain a letter or digit.", nameof(request));

        // Stage every replacement under a new reference. The caller publishes
        // that reference with the connection CAS and removes it on CAS failure,
        // so a stale request can never rotate material used by the live binding.
        var name = $"external-authentication-{request.ConnectionId.ToLowerInvariant()}-{fieldName}-{Guid.NewGuid():N}";
        var secret = await secretManager.CreateAsync(new()
        {
            Name = name,
            DisplayName = $"External authentication {request.FieldName}",
            TypeName = SecretTypeNames.Text,
            StoreName = SecretStoreNames.Encrypted,
            Value = request.Value.Reveal()
        }, cancellationToken);

        return new(ResolverType, secret.Name, Ownership: SecretBindingOwnership.Managed);
    }

    public async ValueTask RemoveAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        EnsureResolverType(binding);
        if (binding.Ownership != SecretBindingOwnership.Managed)
            throw new InvalidOperationException("Only managed secret bindings can remove managed secret material.");
        await secretManager.DeleteAsync(binding.Reference, cancellationToken);
    }

    public async ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        EnsureResolverType(binding);
        var secret = await secretManager.GetAsync(binding.Reference, cancellationToken);
        if (secret is null)
            return new(false, false);

        var configured = secret is { Status: SecretStatus.Active, LatestActiveVersion: not null };
        if (!configured || !IsCompatible(secret, binding))
            return new(configured, false);

        var test = await secretManager.TestAsync(secret.Name, cancellationToken);
        return new(true, test.Succeeded);
    }

    public async ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        EnsureResolverType(binding);
        var secret = await secretManager.GetAsync(binding.Reference, cancellationToken)
            ?? throw new InvalidOperationException("The configured secret binding could not be resolved.");
        if (!IsCompatible(secret, binding))
            throw new InvalidOperationException("The configured secret binding is incompatible with the required type or scope.");
        if (secret is not { Status: SecretStatus.Active, LatestActiveVersion: { } version })
            throw new InvalidOperationException("The configured secret binding is not active.");

        var payload = await secretManager.ResolvePayloadAsync(secret, cancellationToken);
        if (payload.Value is null)
            throw new InvalidOperationException("The configured secret binding could not be resolved.");

        var fingerprint = handleHasher.Hash($"{ResolverType}:{secret.Id}:{version.Version}:{version.CreatedAt.ToUnixTimeMilliseconds()}");
        return new(new(payload.Value), fingerprint);
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
