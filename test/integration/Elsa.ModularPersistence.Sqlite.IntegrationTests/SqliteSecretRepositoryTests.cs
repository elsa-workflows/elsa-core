using Elsa.ModularPersistence.Sqlite.Options;
using Elsa.ModularPersistence.Sqlite.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using Elsa.Secrets.Storage;
using Elsa.Secrets.Stores;
using Elsa.Secrets.Types;
using Microsoft.Extensions.Configuration;

namespace Elsa.ModularPersistence.Sqlite.IntegrationTests;

public class SqliteSecretRepositoryTests : IAsyncDisposable
{
    private readonly string _directory;
    private readonly string _connectionString;

    public SqliteSecretRepositoryTests()
    {
        _directory = Path.Join(Path.GetTempPath(), $"elsa-secrets-vnext-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _connectionString = $"Data Source={Path.Join(_directory, "secrets.db")}";
    }

    [Fact]
    public async Task SecretManagerPreservesBehaviorWithModularPersistenceRepository()
    {
        await MaterializeAsync();
        var manager = CreateManager();

        var created = await manager.CreateAsync(new CreateSecretRequest
        {
            Name = "ApiKey",
            DisplayName = "API Key",
            Description = "Primary API key",
            Scope = "production",
            Tags = ["external"],
            Value = "first-value"
        });
        await manager.CreateAsync(new CreateSecretRequest
        {
            Name = "Webhook",
            DisplayName = "Webhook Secret",
            StoreName = SecretStoreNames.Configuration,
            ConfigurationKey = "Secrets:Webhook"
        });

        var loaded = await manager.GetAsync("APIKEY");
        var productionSecrets = await manager.ListPageAsync(new ListSecretsRequest { Scope = "production", Search = "api" });
        var resolved = await manager.ResolvePayloadAsync("apikey");
        var rotated = await manager.RotateAsync("apikey", new RotateSecretRequest { Value = "second-value" });
        var revoked = await manager.RevokeAsync("apikey");
        var deleted = await manager.DeleteAsync("apikey");
        var deletedLookup = await manager.GetAsync("apikey");
        var recreated = await manager.CreateAsync(new CreateSecretRequest { Name = "ApiKey", DisplayName = "API Key", Value = "third-value" });

        Assert.Equal("apikey", created.Name);
        Assert.NotNull(loaded);
        Assert.Equal("API Key", loaded.DisplayName);
        Assert.Equal("production", loaded.Scope);
        Assert.Equal("external", Assert.Single(loaded.Tags));
        Assert.Equal(1, productionSecrets.TotalCount);
        Assert.Equal("apikey", Assert.Single(productionSecrets.Items).Name);
        Assert.Equal("first-value", resolved.Value);
        Assert.Equal(2, rotated.LatestActiveVersion?.Version);
        Assert.Equal(SecretStatus.Revoked, revoked?.Status);
        Assert.True(deleted);
        Assert.Null(deletedLookup);
        Assert.Equal("apikey", recreated.Name);
        Assert.Equal("third-value", (await manager.ResolvePayloadAsync("apikey")).Value);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);

        return ValueTask.CompletedTask;
    }

    private async ValueTask MaterializeAsync()
    {
        var materializer = new SqliteDocumentSchemaMaterializer(CreateConnectionFactory());
        await materializer.MaterializeAsync(SecretsStorageManifest.Create());
    }

    private DefaultSecretManager CreateManager()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions { EncryptionKey = "0123456789abcdef0123456789abcdef"u8.ToArray() });
        var protector = new DefaultSecretValueProtector(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:Webhook"] = "webhook-value" })
            .Build();
        var stores = new ISecretStore[]
        {
            new EncryptedSecretStore(protector),
            new ConfigurationSecretStore(configuration, options)
        };
        var types = new ISecretTypeProvider[]
        {
            new TextSecretTypeProvider(),
            new RsaKeySecretTypeProvider(),
            new X509CertificateSecretTypeProvider()
        };
        var store = new SqliteDocumentStore(CreateConnectionFactory(), SecretsStorageManifest.Create());
        var repository = new ModularPersistenceSecretRepository(store);

        return new DefaultSecretManager(new DefaultSecretNameValidator(), new SecretStoreRegistry(stores), new SecretTypeRegistry(types), repository);
    }

    private SqliteModularPersistenceConnectionFactory CreateConnectionFactory() =>
        new(new SqliteModularPersistenceOptions { ConnectionString = _connectionString });
}
