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
    public async Task ConfigurationStore_TestAsync_ReturnsFalseWhenConfiguredValueIsMissing()
    {
        var fixture = new SecretTestFixture();
        await fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            StoreName = SecretStoreNames.Configuration,
            ConfigurationKey = "MissingPassword"
        });

        var result = await fixture.Manager.TestAsync("smtp:password");

        Assert.False(result.Succeeded);
        Assert.Equal("Secret value is unavailable.", result.Error);
    }

    [Fact]
    public async Task FileRepository_PersistsSecretAggregate()
    {
        await WithFileRepositoryAsync(async (repository, path) =>
        {
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
        });
    }

    [Fact]
    public async Task FileRepository_SaveAsync_AddsAndUpdatesSecret()
    {
        await WithFileRepositoryAsync(async (repository, _) =>
        {
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Updated password" });
            var reloaded = await repository.GetAsync("smtp:password");

            Assert.NotNull(reloaded);
            Assert.Equal("Updated password", reloaded.DisplayName);
        });
    }

    [Fact]
    public async Task FileRepository_TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await WithFileRepositoryAsync(async (repository, _) =>
        {
            await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });

            var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Active replacement" });
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });
            var deletedReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Replacement password" });
            var reloaded = await repository.GetAsync("smtp:password");

            Assert.False(activeReplacementResult);
            Assert.True(deletedReplacementResult);
            Assert.NotNull(reloaded);
            Assert.Equal("Replacement password", reloaded.DisplayName);
            Assert.Equal(SecretStatus.Active, reloaded.Status);
        });
    }

    [Fact]
    public async Task FileRepository_RecoversFromCorruptJson()
    {
        await WithFileRepositoryAsync(async (repository, path) =>
        {
            await File.WriteAllTextAsync(path, "{not-valid-json");

            var secrets = await repository.ListAsync();
            await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });
            var reloaded = await repository.GetAsync("smtp:password");

            Assert.Empty(secrets);
            Assert.NotNull(reloaded);
        });
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

    private static async Task WithFileRepositoryAsync(Func<FileSecretRepository, string, Task> test)
    {
        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.json");
        try
        {
            var repository = new FileSecretRepository(Microsoft.Extensions.Options.Options.Create(new SecretsOptions { RepositoryFilePath = path }));
            await test(repository, path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
