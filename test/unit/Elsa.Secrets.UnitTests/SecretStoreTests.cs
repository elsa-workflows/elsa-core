using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.UnitTests;

public class SecretStoreTests
{
    [Test]
    public async Task Registries_ExposeBuiltInTypesAndStores()
    {
        var fixture = new SecretTestFixture();

        await Assert.That(fixture.TypeRegistry.List()).Contains(x => x.Name == SecretTypeNames.Text);
        await Assert.That(fixture.TypeRegistry.List()).Contains(x => x.Name == SecretTypeNames.RsaKey);
        await Assert.That(fixture.TypeRegistry.List()).Contains(x => x.Name == SecretTypeNames.X509Certificate);
        await Assert.That(fixture.StoreRegistry.List()).Contains(x => x.Name == SecretStoreNames.Encrypted);
        await Assert.That(fixture.StoreRegistry.List()).Contains(x => x.Name == SecretStoreNames.Configuration);
    }

    [Test]
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

        await Assert.That(value).IsEqualTo("configured-secret");
    }

    [Test]
    public async Task ConfigurationStore_FallsBackToRootConfigurationKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SmtpPassword"] = "root-configured-secret" })
            .Build();
        var fixture = new SecretTestFixture(configuration);

        var secret = await fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            StoreName = SecretStoreNames.Configuration,
            ConfigurationKey = " SmtpPassword "
        });
        var value = await fixture.Resolver.ResolveAsync("smtp:password");

        await Assert.That(value).IsEqualTo("root-configured-secret");
        await Assert.That(secret.Versions.Single().Payload.Value).IsNull();
        await Assert.That(secret.Versions.Single().Payload.Metadata["configurationKey"]).IsEqualTo("SmtpPassword");
    }

    [Test]
    public async Task ConfigurationStore_RotateAsync_UsesReplacementConfigurationKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elsa:Secrets:OldPassword"] = "old-configured-secret",
                ["Elsa:Secrets:NewPassword"] = "new-configured-secret"
            })
            .Build();
        var fixture = new SecretTestFixture(configuration);

        await fixture.Manager.CreateAsync(new CreateSecretRequest
        {
            Name = "smtp:password",
            StoreName = SecretStoreNames.Configuration,
            ConfigurationKey = "OldPassword"
        });
        await fixture.Manager.RotateAsync("smtp:password", new RotateSecretRequest { ConfigurationKey = "NewPassword" });
        var value = await fixture.Resolver.ResolveAsync("smtp:password");

        await Assert.That(value).IsEqualTo("new-configured-secret");
    }

    [Test]
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

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Secret value is unavailable.");
    }

    [Test]
    public async Task Registries_Throw_WhenTypeOrStoreIsMissing()
    {
        var fixture = new SecretTestFixture();

        var missingType = Assert.ThrowsExactly<InvalidOperationException>(() => fixture.TypeRegistry.Get("missing-type"));
        var missingStore = Assert.ThrowsExactly<InvalidOperationException>(() => fixture.StoreRegistry.Get("missing-store"));

        await Assert.That(missingType.Message).Contains("missing-type").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(missingStore.Message).Contains("missing-store").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
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
            var reloaded = await Assert.That(await reloadedRepository.GetAsync("smtp:password")).IsNotNull();

            await Assert.That(reloaded.DisplayName).IsEqualTo("SMTP password");
            await Assert.That(reloaded.Tags).Contains("api-key");
            await Assert.That(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedvalue")).IsTrue();
            await Assert.That(reloaded.Versions.Single().Version).IsEqualTo(1);
        });
    }

    [Test]
    public async Task FileRepository_SaveAsync_AddsAndUpdatesSecret()
    {
        await WithFileRepositoryAsync(async (repository, _) =>
        {
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Updated password" });
            var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

            await Assert.That(reloaded.DisplayName).IsEqualTo("Updated password");
        });
    }

    [Test]
    public async Task FileRepository_TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await WithFileRepositoryAsync(async (repository, _) =>
        {
            await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });

            var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Active replacement" });
            await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });
            var deletedReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "Replacement password" });
            var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

            await Assert.That(activeReplacementResult).IsFalse();
            await Assert.That(deletedReplacementResult).IsTrue();
            await Assert.That(reloaded.DisplayName).IsEqualTo("Replacement password");
            await Assert.That(reloaded.Status).IsEqualTo(SecretStatus.Active);
        });
    }

    [Test]
    public async Task FileRepository_RecoversFromCorruptJson()
    {
        await WithFileRepositoryAsync(async (repository, path) =>
        {
            await File.WriteAllTextAsync(path, "{not-valid-json");

            var secrets = await repository.ListAsync();
            await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });
            var reloaded = await repository.GetAsync("smtp:password");

            await Assert.That(secrets).IsEmpty();
            await Assert.That(reloaded).IsNotNull();
        });
    }

    [Test]
    public async Task InMemoryRepository_ReturnsCopies()
    {
        var repository = new InMemorySecretRepository();
        await repository.AddAsync(new Secret
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Versions = { new SecretVersion { Version = 1, Payload = new SecretPayload { Metadata = { ["protectedValue"] = "ciphertext" } } } }
        });

        var loaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
        loaded.Versions.Clear();
        loaded.DisplayName = "Changed";

        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(reloaded.DisplayName).IsEqualTo("SMTP password");
        await Assert.That(reloaded.Versions).HasSingleItem();
        await Assert.That(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedValue")).IsTrue();
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
