using Elsa.Secrets.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class SecretStoreTests
{
    [Fact]
    public void Registries_ExposeBuiltInTypesAndStores()
    {
        var fixture = new SecretTestFixture();

        Assert.Contains(fixture.TypeRegistry.List(), x => x.Name == SecretTypeNames.Text);
        Assert.Contains(fixture.TypeRegistry.List(), x => x.Name == SecretTypeNames.RsaKey);
        Assert.Contains(fixture.TypeRegistry.List(), x => x.Name == SecretTypeNames.X509Certificate);
        Assert.Contains(fixture.StoreRegistry.List(), x => x.Name == SecretStoreNames.Encrypted);
        Assert.Contains(fixture.StoreRegistry.List(), x => x.Name == SecretStoreNames.Configuration);
    }

    [Fact]
    public async Task ConfigurationStore_ResolvesConfiguredValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Elsa:Secrets:SmtpPassword"] = "configured-secret" })
            .Build();
        var fixture = new SecretTestFixture(configuration);

        await fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            StoreName = SecretStoreNames.Configuration,
            ConfigurationKey = "SmtpPassword"
        });

        var value = await fixture.Resolver.ResolveAsync("smtp:password");

        Assert.Equal("configured-secret", value);
    }
}
