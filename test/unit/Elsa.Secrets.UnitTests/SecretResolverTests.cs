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
}
