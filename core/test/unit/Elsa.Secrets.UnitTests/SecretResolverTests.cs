using Elsa.Secrets.Models;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretResolverTests
{
    private readonly SecretTestFixture _fixture = new();

    [Fact]
    public async Task ResolveAsync_ReturnsLatestActiveVersion()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        await _fixture.Manager.RotateAsync("api:key", new RotateSecretRequest { Value = "two" });

        var value = await _fixture.Resolver.ResolveAsync("api:key");

        Assert.Equal("two", value);
    }

    [Fact]
    public async Task ResolveAsync_ValidatesReferenceType()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", TypeName = SecretTypeNames.Text, Value = "one" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Resolver.ResolveAsync(new SecretReference("api:key", SecretTypeNames.RsaKey)));
    }
    
    [Fact]
    public async Task TestAsync_ReturnsFailedResult_WhenEncryptedPayloadIsMalformed()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        secret.Versions.Single().Payload.Metadata["protectedValue"] = "v1.not-base64.not-base64.not-base64";
        await _fixture.Repository.SaveAsync(secret);

        var result = await _fixture.Manager.TestAsync("api:key");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }
}
