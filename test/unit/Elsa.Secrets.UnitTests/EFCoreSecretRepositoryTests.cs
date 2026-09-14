using System.IO;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Services;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

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
    public async Task Repository_RetainsPreTenancyConstructorShape()
    {
        await Assert.That(typeof(EFCoreSecretRepository).GetConstructor([
            typeof(Store<SecretsElsaDbContext, Secret>),
            typeof(ISecretNameValidator)])).IsNotNull();
        await Assert.That(typeof(EFCoreSecretRepository).GetConstructor([
            typeof(Store<SecretsElsaDbContext, Secret>),
            typeof(ISecretNameValidator),
            typeof(IOptions<TenantsOptions>)])).IsNotNull();
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

        await Assert.That(index.Properties.Select(x => x.Name)).IsEquivalentTo(["TenantId", SecretShadowPropertyNames.NormalizedName], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task DefaultTenantWritesUseEmptyTenantId()
    {
        // Default-tenant uniqueness uses "" so the composite unique index covers these rows.
        // Leftover nulls from SecretTenancy are stamped by SecretDefaultTenantUniqueness.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();

        await repository.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });

        var stored = await Assert.That(await repository.GetAsync("legacy:secret")).IsNotNull();
        await Assert.That(stored.TenantId).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task UniqueIndexRejectsDuplicateDefaultTenantNames()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        await InsertSecretAsync(dbContext, "first", "smtp:password", Tenant.DefaultTenantId);
        await dbContext.SaveChangesAsync();

        await InsertSecretAsync(dbContext, "second", "smtp:password", Tenant.DefaultTenantId);
        await Assert.That(() => dbContext.SaveChangesAsync()).ThrowsExactly<DbUpdateException>();
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

        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
        var whitespaceReloaded = await Assert.That(await repository.GetAsync(" SMTP:PASSWORD ")).IsNotNull();
        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var duplicateException = await Assert.That(() => repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Duplicate password" }))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(reloaded.Name).IsEqualTo("SMTP:PASSWORD");
        await Assert.That(whitespaceReloaded.Id).IsEqualTo(reloaded.Id);
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

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_WhenIncomingTenantDiffers_PreservesExistingTenant()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "old",
                    Name = "smtp:password",
                    DisplayName = "Deleted password",
                    Status = SecretStatus.Deleted
                });

                var replacement = new Secret
                {
                    Id = "new",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Replacement password",
                    TenantId = "tenant-b"
                };
                var result = await repository.TryAddOrReplaceDeletedAsync(replacement);
                var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();

                await Assert.That(result).IsTrue();
                await Assert.That(replacement.TenantId).IsEqualTo("tenant-a");
                await Assert.That(reloaded.Id).IsEqualTo("new");
                await Assert.That(reloaded.DisplayName).IsEqualTo("Replacement password");
                await Assert.That(reloaded.TenantId).IsEqualTo("tenant-a");
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That(await repository.GetAsync("smtp:password")).IsNull();
        });
    }

    [Test]
    public async Task SaveAsync_WhenIncomingTenantDiffers_PreservesExistingTenant()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "old",
                    Name = "smtp:password",
                    DisplayName = "Original"
                });

                await repository.SaveAsync(new Secret
                {
                    Id = "new",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Updated",
                    TenantId = "tenant-b"
                });

                var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
                await Assert.That(reloaded.Id).IsEqualTo("old");
                await Assert.That(reloaded.DisplayName).IsEqualTo("Updated");
                await Assert.That(reloaded.TenantId).IsEqualTo("tenant-a");
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                await Assert.That(await repository.GetAsync("smtp:password")).IsNull();
        });
    }

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_WhenTenancyIsDisabled_PreservesLegacyReplacementBehavior()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.SaveAsync(new Secret
        {
            Id = "old",
            Name = "smtp:password",
            DisplayName = "Deleted password",
            Status = SecretStatus.Deleted,
            TenantId = Tenant.AgnosticTenantId
        });

        var replacement = new Secret
        {
            Id = "new",
            Name = "SMTP:PASSWORD",
            DisplayName = "Replacement password",
            TenantId = "tenant-b"
        };

        await Assert.That(await repository.TryAddOrReplaceDeletedAsync(replacement)).IsTrue();
        var reloaded = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
        await Assert.That(reloaded.TenantId).IsEqualTo("tenant-b");
        await Assert.That(reloaded.Id).IsEqualTo("new");
    }

    [Test]
    public async Task SaveAsync_WhenTenancyIsDisabled_KeepsStoredIdButAcceptsIncomingTenant()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret
        {
            Id = "stored-id",
            Name = "save:secret",
            DisplayName = "Original",
            TenantId = "tenant-a"
        });

        var incoming = new Secret
        {
            Id = "incoming-id",
            Name = " SAVE:SECRET ",
            DisplayName = "Updated",
            TenantId = "tenant-b"
        };
        await repository.SaveAsync(incoming);

        var stored = await Assert.That(await repository.GetAsync("save:secret")).IsNotNull();
        await Assert.That(stored.Id).IsEqualTo("stored-id");
        await Assert.That(stored.TenantId).IsEqualTo("tenant-b");
    }

    [Test]
    public async Task SaveAsync_RejectsNamedWriterUpdatingAgnosticSecretWithoutMutation()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, Tenant.AgnosticTenantId))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "agnostic-id",
                    Name = "smtp:password",
                    DisplayName = "Agnostic secret",
                    TenantId = Tenant.AgnosticTenantId
                });
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var exception = await Assert.That(() => repository.SaveAsync(new Secret
                {
                    Id = "replacement-id",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Forged update"
                })).ThrowsExactly<InvalidOperationException>();

                await Assert.That(exception.Message).IsEqualTo("A secret named 'SMTP:PASSWORD' belongs to another tenant.");
                var unchanged = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
                await Assert.That(unchanged.Id).IsEqualTo("agnostic-id");
                await Assert.That(unchanged.DisplayName).IsEqualTo("Agnostic secret");
                await Assert.That(unchanged.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
            }
        });
    }

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_RejectsNamedWriterReplacingAgnosticSecretWithoutMutation()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, Tenant.AgnosticTenantId))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "agnostic-id",
                    Name = "smtp:password",
                    DisplayName = "Deleted agnostic secret",
                    Status = SecretStatus.Deleted,
                    TenantId = Tenant.AgnosticTenantId
                });
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var result = await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Id = "replacement-id",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Forged replacement"
                });

                await Assert.That(result).IsFalse();
                var unchanged = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
                await Assert.That(unchanged.Id).IsEqualTo("agnostic-id");
                await Assert.That(unchanged.DisplayName).IsEqualTo("Deleted agnostic secret");
                await Assert.That(unchanged.Status).IsEqualTo(SecretStatus.Deleted);
                await Assert.That(unchanged.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
            }
        });
    }

    [Test]
    public async Task TryAddOrReplaceDeletedAsync_AllowsAgnosticWriterReplacingAgnosticSecret()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, Tenant.AgnosticTenantId))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "agnostic-id",
                    Name = "smtp:password",
                    DisplayName = "Deleted agnostic secret",
                    Status = SecretStatus.Deleted,
                    TenantId = Tenant.AgnosticTenantId
                });

                var result = await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Id = "replacement-id",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Replacement agnostic secret"
                });

                await Assert.That(result).IsTrue();
                var replacement = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
                await Assert.That(replacement.Id).IsEqualTo("replacement-id");
                await Assert.That(replacement.DisplayName).IsEqualTo("Replacement agnostic secret");
                await Assert.That(replacement.TenantId).IsEqualTo(Tenant.AgnosticTenantId);
            }
        });
    }

    [Test]
    public async Task PreTenancyConstructor_UsesTenantFilteredModelForOwnershipChecks()
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            using (UseTenant(tenantAccessor, Tenant.AgnosticTenantId))
            {
                await repository.SaveAsync(new Secret
                {
                    Id = "agnostic-id",
                    Name = "smtp:password",
                    DisplayName = "Deleted agnostic secret",
                    Status = SecretStatus.Deleted,
                    TenantId = Tenant.AgnosticTenantId
                });
            }

            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var result = await repository.TryAddOrReplaceDeletedAsync(new Secret
                {
                    Id = "replacement-id",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Forged replacement"
                });

                await Assert.That(result).IsFalse();
                var unchanged = await Assert.That(await repository.GetAsync("smtp:password")).IsNotNull();
                await Assert.That(unchanged.Id).IsEqualTo("agnostic-id");
                await Assert.That(unchanged.Status).IsEqualTo(SecretStatus.Deleted);
            }
        }, useLegacyConstructor: true);
    }

    private static async Task WithTenantAwareRepositoryAsync(Func<EFCoreSecretRepository, DefaultTenantAccessor, Task> test, bool useLegacyConstructor = false)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-secrets-tenant-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        var services = new ServiceCollection()
            .AddSqliteEntityModelCreatingHandlers()
            .AddSingleton<ITenantAccessor>(tenantAccessor)
            .Configure<TenantsOptions>(options => options.IsEnabled = true)
            .AddScoped<IEntitySavingHandler, ApplyTenantId>()
            .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
            .AddDbContextFactory<SecretsElsaDbContext>(builder => builder
                .EnableServiceProviderCaching(false)
                .UseElsaSqlite(typeof(SqliteSecretsPersistenceFeatureExtensions).Assembly, $"Data Source={databasePath}"))
            .Decorate<IDbContextFactory<SecretsElsaDbContext>, TenantAwareDbContextFactory<SecretsElsaDbContext>>()
            .AddSingleton<ISecretNameValidator, DefaultSecretNameValidator>()
            .AddScoped<Store<SecretsElsaDbContext, Secret>>()
            .AddScoped<EFCoreSecretRepository>()
            .BuildServiceProvider();

        try
        {
            await using var scope = services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
            await using (var dbContext = await factory.CreateDbContextAsync())
                await dbContext.Database.MigrateAsync();

            var repository = useLegacyConstructor
                ? new EFCoreSecretRepository(
                    scope.ServiceProvider.GetRequiredService<Store<SecretsElsaDbContext, Secret>>(),
                    scope.ServiceProvider.GetRequiredService<ISecretNameValidator>())
                : scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
            await test(repository, tenantAccessor);
        }
        finally
        {
            await services.DisposeAsync();
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static IDisposable UseTenant(DefaultTenantAccessor tenantAccessor, string tenantId) =>
        tenantAccessor.PushContext(new Tenant { Id = tenantId, Name = tenantId });

    private static async Task InsertSecretAsync(SecretsElsaDbContext dbContext, string id, string name, string tenantId)
    {
        var secret = new Secret
        {
            Id = id,
            Name = name,
            DisplayName = name,
            TenantId = tenantId
        };

        await dbContext.Secrets.AddAsync(secret);
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.NormalizedName).CurrentValue = name;
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.SerializedTags).CurrentValue = "[]";
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.SerializedVersions).CurrentValue = "[]";
    }
}
