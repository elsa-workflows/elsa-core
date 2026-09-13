using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Secrets.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Secrets;

public class ElsaSecretBindingResolverTests
{
    [Test]
    public async Task MissingBindingReportsOnlySafeState()
    {
        var manager = Substitute.For<ISecretManager>();
        manager.GetAsync("missing", Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(null));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());

        var state = await resolver.GetStateAsync(new SecretBinding(ElsaSecretBindingResolver.ResolverType, "missing"));

        await Assert.That(state.IsConfigured).IsFalse();
        await Assert.That(state.IsResolvable).IsFalse();
    }

    [Test]
    public async Task IncompatibleBindingDoesNotResolveOrTestTheSecret()
    {
        var manager = Substitute.For<ISecretManager>();
        var secret = CreateSecret();
        manager.GetAsync(secret.Name, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(secret));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());
        var binding = new SecretBinding(ElsaSecretBindingResolver.ResolverType, secret.Name, "certificate", "other-scope");

        var state = await resolver.GetStateAsync(binding);

        await Assert.That(state.IsConfigured).IsTrue();
        await Assert.That(state.IsResolvable).IsFalse();
        await manager.DidNotReceive().TestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => resolver.ResolveAsync(binding).AsTask());
    }

    [Test]
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
            await Assert.That(state.IsConfigured).IsTrue();
            await Assert.That(state.IsResolvable).IsTrue();
            await Assert.That(first.Value.Reveal()).IsEqualTo("super-secret");
            await Assert.That(sameGeneration.GenerationFingerprint).IsEqualTo(first.GenerationFingerprint);
            await Assert.That(rotated.GenerationFingerprint).IsNotEqualTo(first.GenerationFingerprint);
            await Assert.That(first.GenerationFingerprint).DoesNotContain("super-secret").WithComparison(StringComparison.Ordinal);
        }
        finally
        {
            first.Value.Dispose();
            sameGeneration.Value.Dispose();
            rotated.Value.Dispose();
        }
    }

    [Test]
    public async Task ManagedReplaceStagesUniqueManagedBindingWithoutExposingTheValue()
    {
        var manager = Substitute.For<ISecretManager>();
        manager.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Secret?>(null));
        manager.CreateAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult(new Secret { Name = call.Arg<CreateSecretRequest>().Name }));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());

        using var value = new SensitiveString("super-secret");
        var binding = await resolver.StageAsync(new ManagedSecretBindingWriteRequest("connection-a", "clientSecret", value));
        var replacementBinding = await resolver.StageAsync(new ManagedSecretBindingWriteRequest("connection-a", "clientSecret", value));

        await Assert.That(binding.Ownership).IsEqualTo(SecretBindingOwnership.Managed);
        await Assert.That(binding.ResolverType).IsEqualTo(ElsaSecretBindingResolver.ResolverType);
        await Assert.That(binding.Reference).StartsWith("external-authentication:").WithComparison(StringComparison.Ordinal);
        await Assert.That(Guid.TryParseExact(binding.Reference["external-authentication:".Length..], "N", out _)).IsTrue();
        await Assert.That(replacementBinding.Reference).IsNotEqualTo(binding.Reference);
        await Assert.That(binding.Reference).DoesNotContain("super-secret").WithComparison(StringComparison.Ordinal);
        await manager.Received(2).CreateAsync(Arg.Is<CreateSecretRequest>(x => x.Value == "super-secret"), Arg.Any<CancellationToken>());
        await manager.DidNotReceive().RotateAsync(Arg.Any<string>(), Arg.Any<RotateSecretRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemovingManagedBindingDeletesOnlyStudioOwnedSecretMaterial()
    {
        var manager = Substitute.For<ISecretManager>();
        manager.DeleteAsync("managed-secret", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var resolver = new ElsaSecretBindingResolver(manager, new TestHasher());

        await resolver.RemoveAsync(new SecretBinding(ElsaSecretBindingResolver.ResolverType, "managed-secret", Ownership: SecretBindingOwnership.Managed));

        await manager.Received(1).DeleteAsync("managed-secret", Arg.Any<CancellationToken>());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => resolver.RemoveAsync(new SecretBinding(ElsaSecretBindingResolver.ResolverType, "deployment-secret")).AsTask());
        await manager.DidNotReceive().DeleteAsync("deployment-secret", Arg.Any<CancellationToken>());
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
