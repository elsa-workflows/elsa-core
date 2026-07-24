using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Secrets.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using NSubstitute;

namespace Elsa.ExternalAuthentication.UnitTests.Secrets;

public class ElsaSecretBindingResolverTests
{
    [Fact]
    public async Task MissingBindingReportsOnlySafeState()
    {
        var manager = Substitute.For<ISecretManager>();
        manager.GetAsync("missing", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(null));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());

        var state = await resolver.GetStateAsync(new SecretBinding(ElsaSecretBindingResolver.ResolverType, "missing"));

        Assert.False(state.IsConfigured);
        Assert.False(state.IsResolvable);
    }

    [Fact]
    public async Task IncompatibleBindingDoesNotResolveOrTestTheSecret()
    {
        var manager = Substitute.For<ISecretManager>();
        var secret = CreateSecret();
        manager.GetAsync(secret.Name, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(secret));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());
        var binding = new SecretBinding(ElsaSecretBindingResolver.ResolverType, secret.Name, "certificate", "other-scope");

        var state = await resolver.GetStateAsync(binding);

        Assert.True(state.IsConfigured);
        Assert.False(state.IsResolvable);
        await manager.DidNotReceive().TestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(binding).AsTask());
    }

    [Fact]
    public async Task ResolutionReturnsTransientValueAndOpaqueStableGenerationThatChangesOnRotation()
    {
        var manager = Substitute.For<ISecretManager>();
        var secret = CreateSecret();
        manager.GetAsync(secret.Name, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(secret));
        manager.TestAsync(secret.Name, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SecretTestResponse { Succeeded = true }));
        manager.ResolvePayloadAsync(secret, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SecretPayload { Value = "super-secret" }));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());
        var binding = new SecretBinding(ElsaSecretBindingResolver.ResolverType, secret.Name, SecretTypeNames.Text, "external-authentication");

        var state = await resolver.GetStateAsync(binding);
        var first = await resolver.ResolveAsync(binding);
        var sameGeneration = await resolver.ResolveAsync(binding);
        secret.Versions.Add(new SecretVersion
        {
            Version = 2,
            CreatedAt = DateTimeOffset.Parse("2026-07-24T12:00:00Z"),
            Payload = new SecretPayload()
        });
        secret.Versions[0].Status = SecretStatus.Retired;
        var rotated = await resolver.ResolveAsync(binding);
        try
        {
            Assert.True(state.IsConfigured);
            Assert.True(state.IsResolvable);
            Assert.Equal("super-secret", first.Value.Reveal());
            Assert.Equal(first.GenerationFingerprint, sameGeneration.GenerationFingerprint);
            Assert.NotEqual(first.GenerationFingerprint, rotated.GenerationFingerprint);
            Assert.DoesNotContain("super-secret", first.GenerationFingerprint, StringComparison.Ordinal);
        }
        finally
        {
            first.Value.Dispose();
            sameGeneration.Value.Dispose();
            rotated.Value.Dispose();
        }
    }

    private static Secret CreateSecret() => new()
    {
        Id = "secret-id",
        Name = "contoso-client-secret",
        DisplayName = "Contoso client secret",
        TypeName = SecretTypeNames.Text,
        Scope = "external-authentication",
        Status = SecretStatus.Active,
        Versions =
        [
            new SecretVersion
            {
                Version = 1,
                CreatedAt = DateTimeOffset.Parse("2026-07-01T12:00:00Z"),
                Payload = new SecretPayload()
            }
        ]
    };

    private sealed class TestHasher : IExternalAuthenticationHandleHasher
    {
        public string Hash(string value) => $"fingerprint:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)))}";
    }
}
