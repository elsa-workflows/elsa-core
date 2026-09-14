using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Repositories;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using Elsa.Tenants.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// One File, InMemory, or EF/SQLite <see cref="ISecretRepository"/> plus the ambient tenant it reads.
/// </summary>
public sealed class SecretRepositoryScenario(
    ITenantAccessor tenantAccessor,
    ISecretRepository repository,
    Func<ValueTask> disposeAsync) : IAsyncDisposable
{
    public ITenantAccessor TenantAccessor { get; } = tenantAccessor;
    public ISecretRepository Repository { get; } = repository;

    public IDisposable UseTenant(string tenantId) =>
        TenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId
            ? Tenant.Default
            : new Tenant
            {
                Id = tenantId,
                Name = tenantId
            });

    public ValueTask DisposeAsync() => disposeAsync();

    public static Task<SecretRepositoryScenario> CreateInMemoryAsync()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        return Task.FromResult(new SecretRepositoryScenario(
            tenantAccessor,
            new InMemorySecretRepository(tenantAccessor),
            () => ValueTask.CompletedTask));
    }

    public static Task<SecretRepositoryScenario> CreateFileAsync()
    {
        var path = Path.Join(Path.GetTempPath(), $"elsa-secrets-conformance-{Guid.NewGuid():N}.json");
        var tenantAccessor = new DefaultTenantAccessor();
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions
        {
            RepositoryFilePath = path
        });

        return Task.FromResult(new SecretRepositoryScenario(
            tenantAccessor,
            new FileSecretRepository(options, tenantAccessor: tenantAccessor),
            () =>
            {
                if (File.Exists(path))
                    File.Delete(path);
                return ValueTask.CompletedTask;
            }));
    }

    public static async Task<SecretRepositoryScenario> CreateSqliteAsync()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-secrets-conformance-{Guid.NewGuid():N}.db");
        var tenantAccessor = new DefaultTenantAccessor();
        ServiceProvider? services = null;
        IServiceScope? scope = null;

        try
        {
            var migrationsAssembly = typeof(SqliteSecretsPersistenceFeatureExtensions).Assembly;
            services = new ServiceCollection()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .Configure<TenantsOptions>(options => options.IsEnabled = true)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<SecretsElsaDbContext>((_, builder) =>
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={databasePath}")
                        .ReplaceService<IModelCacheKeyFactory, TenantAwareSecretModelCacheKeyFactory>())
                .Decorate<IDbContextFactory<SecretsElsaDbContext>, TenantAwareDbContextFactory<SecretsElsaDbContext>>()
                .AddSingleton<ISecretNameValidator, DefaultSecretNameValidator>()
                .AddScoped<Store<SecretsElsaDbContext, Secret>>()
                .AddScoped<EFCoreSecretRepository>()
                .BuildServiceProvider();

            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync())
                await dbContext.Database.MigrateAsync();

            scope = services.CreateScope();
            return new(
                tenantAccessor,
                scope.ServiceProvider.GetRequiredService<EFCoreSecretRepository>(),
                async () =>
                {
                    scope.Dispose();
                    await services.DisposeAsync();
                    SqliteConnection.ClearAllPools();
                    if (File.Exists(databasePath))
                        File.Delete(databasePath);
                });
        }
        catch
        {
            scope?.Dispose();
            if (services is not null)
                await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
                File.Delete(databasePath);
            throw;
        }
    }
}

/// <summary>
/// Keeps the tenant-aware Secrets model out of the default EF cache shared with
/// <c>EFCoreSecretRepositoryTests</c>, which builds the same context type without a query filter.
/// </summary>
file sealed class TenantAwareSecretModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), designTime, "secrets-tenant-aware");
}
