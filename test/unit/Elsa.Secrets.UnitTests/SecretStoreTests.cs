using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
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

    [Fact]
    public async Task FileRepository_PersistsSecretAggregate()
    {
        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var repository = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));
            var secret = new Secret
            {
                Name = "smtp:password",
                DisplayName = "SMTP password",
                Tags = ["API-Key"],
                Versions = { new SecretVersion { Version = 1, Payload = new SecretPayload { Value = "stored", Metadata = { ["ProtectedValue"] = "ciphertext" } } } }
            };

            await repository.AddAsync(secret);

            var reloadedRepository = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));
            var reloaded = await reloadedRepository.GetAsync("smtp:password");

            Assert.NotNull(reloaded);
            Assert.Equal("SMTP password", reloaded.DisplayName);
            Assert.Contains("api-key", reloaded.Tags);
            Assert.True(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedvalue"));
            Assert.Equal(1, reloaded.Versions.Single().Version);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task InMemoryRepository_ReturnsCopies()
    {
        var repository = new InMemorySecretRepository();
        await repository.AddAsync(new Secret
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Versions = { new SecretVersion { Version = 1, Payload = new SecretPayload { Metadata = { ["protectedValue"] = "ciphertext" } } } }
        });

        var loaded = await repository.GetAsync("smtp:password");
        loaded!.Versions.Clear();
        loaded.DisplayName = "Changed";

        var reloaded = await repository.GetAsync("smtp:password");

        Assert.Equal("SMTP password", reloaded!.DisplayName);
        Assert.Single(reloaded.Versions);
        Assert.True(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedValue"));
    }
}
