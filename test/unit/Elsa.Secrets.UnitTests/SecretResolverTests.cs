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
    public async Task ProviderAdapter_ReturnsNull_WhenSecretIsUnavailable()
    {
        await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        await _fixture.Manager.RevokeAsync("api:key");
        var provider = new Elsa.Secrets.Services.SecretProviderAdapter(_fixture.Resolver);

        var value = await provider.GetSecretAsync("api:key");

        Assert.Null(value);
    }

    [Fact]
    public async Task ProviderAdapter_ReturnsNull_WhenEncryptedPayloadCannotBeDecrypted()
    {
        var secret = await _fixture.Manager.CreateAsync(new CreateSecretRequest { Name = "api:key", Value = "one" });
        var version = secret.Versions.Single();
        version.Payload.Metadata["protectedValue"] = string.Join(
            ".",
            "v1",
            Convert.ToBase64String(new byte[12]),
            Convert.ToBase64String(new byte[16]),
            Convert.ToBase64String(new byte[1]));
        await _fixture.Repository.SaveAsync(secret);
        var provider = new Elsa.Secrets.Services.SecretProviderAdapter(_fixture.Resolver);

        var value = await provider.GetSecretAsync("api:key");

        Assert.Null(value);
    }
}
