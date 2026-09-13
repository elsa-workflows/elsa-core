using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.UnitTests;

public class EFCoreSecretRepositoryTests
{
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-secrets-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _serviceProvider;

    public EFCoreSecretRepositoryTests()
    {
        var services = new ServiceCollection();
        var connectionString = $"Data Source={_databasePath}";
        services.AddSqliteEntityModelCreatingHandlers();
        services.AddDbContextFactory<SecretsElsaDbContext>(builder => builder.UseElsaSqlite(typeof(SqliteSecretsPersistenceFeatureExtensions).Assembly, connectionString));
        services.AddSingleton<ISecretNameValidator, DefaultSecretNameValidator>();
        services.AddScoped<Store<SecretsElsaDbContext, Secret>>();
        services.AddScoped<EFCoreSecretRepository>();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    [After(Test)]
    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [Test]
    public async Task SecretNamesAreUniquePerTenantRatherThanGlobally()
    {
        // Asserted against the schema rather than through the repository. The repository's own duplicate-name
        // check queries dbContext.Secrets, which the global query filter already scopes to the ambient tenant
        // when multitenancy is on -- so exercising it here would test SetTenantIdFilter, not this change. What
        // this change owns is the index, and a global unique index would make the name a shared resource: the
        // second tenant to want "smtp:password" could not create one.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        var index = dbContext.Model
            .FindEntityType(typeof(Secret))!
            .GetIndexes()
            .Single(x => x.IsUnique);

        await Assert.That(index.Properties.Select(x => x.Name)).IsEquivalentTo(
            ["TenantId", SecretShadowPropertyNames.NormalizedName],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ExistingSecretsCarryNoTenantUntilOneIsAssigned()
    {
        // The upgrade adds the column nullable with no backfill, so rows written before it stay null. That is
        // what SetTenantIdFilter's "null counts as the default tenant" clause is for, and it is why the
        // migration needs no data step.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();

        await repository.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });

        var stored = await Assert.That(await repository.GetAsync("legacy:secret")).IsNotNull();
        await Assert.That(stored.TenantId).IsNull();
    }

    [Test]
    public async Task PersistsSecretAggregate()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        var secret = new Secret
        {
            Name = "smtp:password",
            DisplayName = "SMTP password",
            Tags = ["API-Key"],
            Versions = { new SecretVersion { Version = 1, Payload = new SecretPayload { Metadata = { ["ProtectedValue"] = "ciphertext" } } } }
        };

        await repository.AddAsync(secret);
        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(reloaded.DisplayName).IsEqualTo("SMTP password");
        await Assert.That(reloaded.Tags).Contains("api-key");
        await Assert.That(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedvalue")).IsTrue();
        await Assert.That(reloaded.Versions.Single().Version).IsEqualTo(1);
    }

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });

        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Active replacement" });
        await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });
        var deletedReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(activeReplacementResult).IsFalse();
        await Assert.That(deletedReplacementResult).IsTrue();
        await Assert.That(reloaded.DisplayName).IsEqualTo("Replacement password");
        await Assert.That(reloaded.Status).IsEqualTo(SecretStatus.Active);
    }

    [Test]
    public async Task NameLookups_AreCaseInsensitive()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "SMTP password" });

        var reloaded = await repository.GetAsync("smtp:password");
        var whitespaceReloaded = await repository.GetAsync(" SMTP:PASSWORD ");
        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var duplicateException = await Assert.That(
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Duplicate password" }))).IsNotNull();

        var nonNullReloaded = await Assert.That(reloaded).IsNotNull();
        var nonNullWhitespaceReloaded = await Assert.That(whitespaceReloaded).IsNotNull();
        await Assert.That(nonNullReloaded.Name).IsEqualTo("SMTP:PASSWORD");
        await Assert.That(nonNullWhitespaceReloaded.Id).IsEqualTo(nonNullReloaded.Id);
        await Assert.That(activeReplacementResult).IsFalse();
        await Assert.That(duplicateException.Message).IsEqualTo("A secret named 'smtp:password' already exists.");
    }

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_WhenReplacingDeletedSecret_PersistsReplacementId()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.SaveAsync(new Secret { Id = "old", Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });

        var replacement = new Secret { Id = "new", Name = "SMTP:PASSWORD", DisplayName = "Replacement password" };
        var result = await repository.TryAddOrReplaceDeletedAsync(replacement);
        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

        await Assert.That(result).IsTrue();
        await Assert.That(reloaded.Id).IsEqualTo("new");
        await Assert.That(reloaded.DisplayName).IsEqualTo("Replacement password");
        await Assert.That(reloaded.Status).IsEqualTo(SecretStatus.Active);
    }
}
