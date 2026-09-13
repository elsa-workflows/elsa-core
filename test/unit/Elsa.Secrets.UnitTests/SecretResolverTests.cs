using Elsa.Secrets.Models;

namespace Elsa.Secrets.UnitTests;

public class SecretResolverTests
{
    private readonly SecretTestFixture _fixture = new();

    [Test]
    public async Task ResolveAsync_ReturnsLatestActiveVersion()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        await _fixture.Manager.RotateAsync("api:key", new RotateSecretRequest { Value = "two" });

        var value = await _fixture.Resolver.ResolveAsync("api:key");

        await Assert.That(value).IsEqualTo("two");
    }

    [Test]
    public async Task ResolveAsync_ValidatesReferenceType()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", TypeName = SecretTypeNames.Text, Value = "one" });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _fixture.Resolver.ResolveAsync(new SecretReference("api:key", SecretTypeNames.RsaKey)));
    }
    
    [Test]
    public async Task TestAsync_ReturnsFailedResult_WhenEncryptedPayloadIsMalformed()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        secret.Versions.Single().Payload.Metadata["protectedValue"] = "v1.not-base64.not-base64.not-base64";
        await _fixture.Repository.SaveAsync(secret);

        var result = await _fixture.Manager.TestAsync("api:key");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Error).IsNotNull();
    }
}
