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
using Xunit;

namespace Elsa.Secrets.UnitTests;

public class EFCoreSecretRepositoryTests : IAsyncLifetime
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

    public async Task InitializeAsync()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [Fact]
    public void Repository_RetainsPreTenancyConstructorShape()
    {
        Assert.NotNull(typeof(EFCoreSecretRepository).GetConstructor([
            typeof(Store<SecretsElsaDbContext, Secret>),
            typeof(ISecretNameValidator)]));
        Assert.NotNull(typeof(EFCoreSecretRepository).GetConstructor([
            typeof(Store<SecretsElsaDbContext, Secret>),
            typeof(ISecretNameValidator),
            typeof(IOptions<TenantsOptions>)]));
    }

    [Fact]
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

        Assert.Equal(["TenantId", SecretShadowPropertyNames.NormalizedName], index.Properties.Select(x => x.Name));
    }

    [Fact]
    public async Task DefaultTenantWritesUseEmptyTenantId()
    {
        // Default-tenant uniqueness uses "" so the composite unique index covers these rows.
        // Leftover nulls from SecretTenancy are stamped by SecretDefaultTenantUniqueness.
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();

        await repository.AddAsync(new Secret { Name = "legacy:secret", DisplayName = "Legacy" });

        var stored = await repository.GetAsync("legacy:secret");
        Assert.NotNull(stored);
        Assert.Equal(string.Empty, stored!.TenantId);
    }

    [Fact]
    public async Task UniqueIndexRejectsDuplicateDefaultTenantNames()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();

        await InsertSecretAsync(dbContext, "first", "smtp:password", Tenant.DefaultTenantId);
        await dbContext.SaveChangesAsync();

        await InsertSecretAsync(dbContext, "second", "smtp:password", Tenant.DefaultTenantId);
        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
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
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.NotNull(reloaded);
        Assert.Equal("SMTP password", reloaded.DisplayName);
        Assert.Contains("api-key", reloaded.Tags);
        Assert.True(reloaded.Versions.Single().Payload.Metadata.ContainsKey("protectedvalue"));
        Assert.Equal(1, reloaded.Versions.Single().Version);
    }

    [Fact]
    public async Task TryAddOrReplaceDeletedAsync_ReplacesOnlyDeletedSecret()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "SMTP password" });

        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Active replacement" });
        await repository.SaveAsync(new Secret { Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });
        var deletedReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.False(activeReplacementResult);
        Assert.True(deletedReplacementResult);
        Assert.NotNull(reloaded);
        Assert.Equal("Replacement password", reloaded.DisplayName);
        Assert.Equal(SecretStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task NameLookups_AreCaseInsensitive()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.AddAsync(new Secret { Name = "SMTP:PASSWORD", DisplayName = "SMTP password" });

        var reloaded = await repository.GetAsync("smtp:password");
        var whitespaceReloaded = await repository.GetAsync(" SMTP:PASSWORD ");
        var activeReplacementResult = await repository.TryAddOrReplaceDeletedAsync(new Secret { Name = "smtp:password", DisplayName = "Replacement password" });
        var duplicateException = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(new Secret { Name = "smtp:password", DisplayName = "Duplicate password" }));

        Assert.NotNull(reloaded);
        Assert.NotNull(whitespaceReloaded);
        Assert.Equal("SMTP:PASSWORD", reloaded.Name);
        Assert.Equal(reloaded.Id, whitespaceReloaded.Id);
        Assert.False(activeReplacementResult);
        Assert.Equal("A secret named 'smtp:password' already exists.", duplicateException.Message);
    }

    [Fact]
    public async Task TryAddOrReplaceDeletedAsync_WhenReplacingDeletedSecret_PersistsReplacementId()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>();
        await repository.SaveAsync(new Secret { Id = "old", Name = "smtp:password", DisplayName = "Deleted password", Status = SecretStatus.Deleted });

        var replacement = new Secret { Id = "new", Name = "SMTP:PASSWORD", DisplayName = "Replacement password" };
        var result = await repository.TryAddOrReplaceDeletedAsync(replacement);
        var reloaded = await repository.GetAsync("smtp:password");

        Assert.True(result);
        Assert.NotNull(reloaded);
        Assert.Equal("new", reloaded.Id);
        Assert.Equal("Replacement password", reloaded.DisplayName);
        Assert.Equal(SecretStatus.Active, reloaded.Status);
    }

    [Fact]
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
                var reloaded = await repository.GetAsync("smtp:password");

                Assert.True(result);
                Assert.Equal("tenant-a", replacement.TenantId);
                Assert.NotNull(reloaded);
                Assert.Equal("new", reloaded!.Id);
                Assert.Equal("Replacement password", reloaded.DisplayName);
                Assert.Equal("tenant-a", reloaded.TenantId);
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Null(await repository.GetAsync("smtp:password"));
        });
    }

    [Fact]
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

                var reloaded = await repository.GetAsync("smtp:password");
                Assert.NotNull(reloaded);
                Assert.Equal("old", reloaded!.Id);
                Assert.Equal("Updated", reloaded.DisplayName);
                Assert.Equal("tenant-a", reloaded.TenantId);
            }

            using (UseTenant(tenantAccessor, "tenant-b"))
                Assert.Null(await repository.GetAsync("smtp:password"));
        });
    }

    [Theory]
    [InlineData("add", "tenant-b")]
    [InlineData("save", "tenant-b")]
    [InlineData("try-add-or-replace", "tenant-b")]
    [InlineData("add", Tenant.AgnosticTenantId)]
    [InlineData("save", Tenant.AgnosticTenantId)]
    [InlineData("try-add-or-replace", Tenant.AgnosticTenantId)]
    public async Task NewSecretCannotBeInsertedForAnotherTenant(string operation, string targetTenantId)
    {
        await WithTenantAwareRepositoryAsync(async (repository, tenantAccessor) =>
        {
            var name = $"novel:{operation}:{targetTenantId}";
            using (UseTenant(tenantAccessor, "tenant-a"))
            {
                var secret = new Secret
                {
                    Id = "forged-secret",
                    Name = name,
                    DisplayName = "Forged secret",
                    TenantId = targetTenantId
                };

                if (operation == "try-add-or-replace")
                    Assert.False(await repository.TryAddOrReplaceDeletedAsync(secret));
                else
                {
                    var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        operation == "add" ? repository.AddAsync(secret) : repository.SaveAsync(secret));
                    Assert.Equal($"A secret named '{name}' belongs to another tenant.", error.Message);
                }

                Assert.Null(await repository.GetAsync(name));
            }

            using (UseTenant(tenantAccessor, targetTenantId))
                Assert.Null(await repository.GetAsync(name));
        });
    }

    [Fact]
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

        Assert.True(await repository.TryAddOrReplaceDeletedAsync(replacement));
        var reloaded = await repository.GetAsync("smtp:password");
        Assert.NotNull(reloaded);
        Assert.Equal("tenant-b", reloaded!.TenantId);
        Assert.Equal("new", reloaded.Id);
    }

    [Fact]
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

        var stored = await repository.GetAsync("save:secret");
        Assert.NotNull(stored);
        Assert.Equal("stored-id", stored!.Id);
        Assert.Equal("tenant-b", stored.TenantId);
    }

    [Fact]
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
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(new Secret
                {
                    Id = "replacement-id",
                    Name = "SMTP:PASSWORD",
                    DisplayName = "Forged update"
                }));

                Assert.Equal("A secret named 'SMTP:PASSWORD' belongs to another tenant.", exception.Message);
                var unchanged = await repository.GetAsync("smtp:password");
                Assert.NotNull(unchanged);
                Assert.Equal("agnostic-id", unchanged!.Id);
                Assert.Equal("Agnostic secret", unchanged.DisplayName);
                Assert.Equal(Tenant.AgnosticTenantId, unchanged.TenantId);
            }
        });
    }

    [Fact]
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

                Assert.False(result);
                var unchanged = await repository.GetAsync("smtp:password");
                Assert.NotNull(unchanged);
                Assert.Equal("agnostic-id", unchanged!.Id);
                Assert.Equal("Deleted agnostic secret", unchanged.DisplayName);
                Assert.Equal(SecretStatus.Deleted, unchanged.Status);
                Assert.Equal(Tenant.AgnosticTenantId, unchanged.TenantId);
            }
        });
    }

    [Fact]
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

                Assert.True(result);
                var replacement = await repository.GetAsync("smtp:password");
                Assert.NotNull(replacement);
                Assert.Equal("replacement-id", replacement!.Id);
                Assert.Equal("Replacement agnostic secret", replacement.DisplayName);
                Assert.Equal(Tenant.AgnosticTenantId, replacement.TenantId);
            }
        });
    }

    [Fact]
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

                Assert.False(result);
                var unchanged = await repository.GetAsync("smtp:password");
                Assert.NotNull(unchanged);
                Assert.Equal("agnostic-id", unchanged!.Id);
                Assert.Equal(SecretStatus.Deleted, unchanged.Status);
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
