using System.Text.Json;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Stores;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Persistence.ConformanceTests;

/// <summary>
/// Holds one Memory or EF/SQLite pair of Alterations stores and the ambient tenant they read.
/// </summary>
public sealed class AlterationStoreScenario(
    TestTenantAccessor tenantAccessor,
    IAlterationPlanStore plans,
    IAlterationJobStore jobs,
    Func<ValueTask> disposeAsync) : IAsyncDisposable
{
    public TestTenantAccessor TenantAccessor { get; } = tenantAccessor;
    public IAlterationPlanStore Plans { get; } = plans;
    public IAlterationJobStore Jobs { get; } = jobs;

    public IDisposable UseTenant(string tenantId) =>
        TenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId
            ? Tenant.Default
            : new Tenant { Id = tenantId, Name = tenantId });

    public ValueTask DisposeAsync() => disposeAsync();

    public static Task<AlterationStoreScenario> CreateInMemoryAsync()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var plans = new MemoryStore<AlterationPlan>();
        var jobs = new MemoryStore<AlterationJob>();

        return Task.FromResult(new AlterationStoreScenario(
            tenantAccessor,
            new MemoryAlterationPlanStore(plans, tenantAccessor),
            new MemoryAlterationJobStore(jobs, tenantAccessor),
            () => ValueTask.CompletedTask));
    }

    public static Task<AlterationStoreScenario> CreateSqliteAsync() =>
        CreateSqliteAsync("tenant-a");

    public static Task<AlterationStoreScenario> CreateSqliteAsync(string tenantId) =>
        CreateSqliteAsync(tenantId, Path.Join(Path.GetTempPath(), $"elsa-alterations-conformance-{Guid.NewGuid():N}.db"), ownsDatabaseFile: true);

    public static Task<AlterationStoreScenario> CreateSqliteAsync(
        string tenantId,
        bool tenantsEnabled,
        DbCommandInterceptor? commandInterceptor = null,
        IDbExceptionHandler? dbExceptionHandler = null,
        DbTransactionInterceptor? transactionInterceptor = null) =>
        CreateSqliteAsync(
            tenantId,
            Path.Join(Path.GetTempPath(), $"elsa-alterations-conformance-{Guid.NewGuid():N}.db"),
            ownsDatabaseFile: true,
            tenantsEnabled,
            commandInterceptor,
            dbExceptionHandler,
            transactionInterceptor);

    /// <summary>
    /// Two EF/SQLite hosts that share a file and keep separate ambient tenants so concurrent
    /// Save/SaveMany calls do not mutate a single <see cref="ITenantAccessor"/>.
    /// </summary>
    public static async Task<SqliteOwnershipPair> CreateSqlitePairAsync(
        string firstTenantId,
        string secondTenantId,
        DbCommandInterceptor? commandInterceptor = null,
        DbTransactionInterceptor? transactionInterceptor = null)
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-alterations-ownership-{Guid.NewGuid():N}.db");
        var first = await CreateSqliteAsync(
            firstTenantId,
            databasePath,
            ownsDatabaseFile: false,
            commandInterceptor: commandInterceptor,
            transactionInterceptor: transactionInterceptor);
        try
        {
            var second = await CreateSqliteAsync(
                secondTenantId,
                databasePath,
                ownsDatabaseFile: false,
                commandInterceptor: commandInterceptor,
                transactionInterceptor: transactionInterceptor);
            return new SqliteOwnershipPair(first, second, databasePath);
        }
        catch
        {
            await first.DisposeAsync();
            throw;
        }
    }

    public static Task<AlterationStoreScenario> CreateSqliteAsync(
        string tenantId,
        string databasePath,
        bool ownsDatabaseFile,
        bool tenantsEnabled = true,
        DbCommandInterceptor? commandInterceptor = null,
        IDbExceptionHandler? dbExceptionHandler = null,
        DbTransactionInterceptor? transactionInterceptor = null) =>
        CreateSqliteHostAsync(tenantId, databasePath, ownsDatabaseFile, tenantsEnabled, commandInterceptor, dbExceptionHandler, transactionInterceptor);

    private static async Task<AlterationStoreScenario> CreateSqliteHostAsync(
        string tenantId,
        string databasePath,
        bool ownsDatabaseFile,
        bool tenantsEnabled,
        DbCommandInterceptor? commandInterceptor,
        IDbExceptionHandler? dbExceptionHandler,
        DbTransactionInterceptor? transactionInterceptor)
    {
        var tenantAccessor = new TestTenantAccessor(tenantId);
        ServiceProvider? services = null;
        IServiceScope? scope = null;

        try
        {
            var migrationsAssembly = typeof(AlterationsDbContextFactories).Assembly;
            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .AddSingleton<IAlterationSerializer, ConformanceAlterationSerializer>()
                .Configure<TenantsOptions>(options => options.IsEnabled = tenantsEnabled)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<AlterationsElsaDbContext>((_, builder) =>
                {
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={databasePath};Default Timeout=30");
                    builder.EnableServiceProviderCaching(false);
                    if (commandInterceptor is not null)
                        builder.AddInterceptors(commandInterceptor);
                    if (transactionInterceptor is not null)
                        builder.AddInterceptors(transactionInterceptor);
                })
                .Decorate<IDbContextFactory<AlterationsElsaDbContext>, TenantAwareDbContextFactory<AlterationsElsaDbContext>>()
                .AddScoped<EntityStore<AlterationsElsaDbContext, AlterationPlan>>()
                .AddScoped<EntityStore<AlterationsElsaDbContext, AlterationJob>>()
                .AddScoped<EFCoreAlterationPlanStore>()
                .AddScoped<EFCoreAlterationJobStore>();

            if (dbExceptionHandler is not null)
                serviceCollection.AddSingleton(dbExceptionHandler);

            services = serviceCollection.BuildServiceProvider();

            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<AlterationsElsaDbContext>>().CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            scope = services.CreateScope();
            var scoped = scope.ServiceProvider;

            return new(
                tenantAccessor,
                scoped.GetRequiredService<EFCoreAlterationPlanStore>(),
                scoped.GetRequiredService<EFCoreAlterationJobStore>(),
                async () =>
                {
                    scope.Dispose();
                    await services.DisposeAsync();
                    if (ownsDatabaseFile)
                    {
                        SqliteConnection.ClearAllPools();
                        File.Delete(databasePath);
                    }
                });
        }
        catch
        {
            scope?.Dispose();
            if (services is not null)
                await services.DisposeAsync();
            if (ownsDatabaseFile)
            {
                SqliteConnection.ClearAllPools();
                File.Delete(databasePath);
            }

            throw;
        }
    }

    /// <summary>
    /// JSON stand-in for the real alteration serializer so EF can persist contract-visible fields.
    /// </summary>
    private sealed class ConformanceAlterationSerializer : IAlterationSerializer
    {
        public string Serialize(IAlteration alteration) => JsonSerializer.Serialize((TestAlteration)alteration);

        public string SerializeMany(IEnumerable<IAlteration> alterations) =>
            JsonSerializer.Serialize(alterations.Cast<TestAlteration>().ToArray());

        public IAlteration Deserialize(string json) => JsonSerializer.Deserialize<TestAlteration>(json)!;

        public IEnumerable<IAlteration> DeserializeMany(string json) =>
            JsonSerializer.Deserialize<TestAlteration[]>(json)!;
    }
}

/// <summary>
/// Two SQLite alteration-store hosts that share one database file.
/// </summary>
public sealed class SqliteOwnershipPair(AlterationStoreScenario first, AlterationStoreScenario second, string databasePath) : IAsyncDisposable
{
    public AlterationStoreScenario First { get; } = first;
    public AlterationStoreScenario Second { get; } = second;
    public string DatabasePath { get; } = databasePath;

    public async ValueTask DisposeAsync()
    {
        await First.DisposeAsync();
        await Second.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(DatabasePath);
    }
}

/// <summary>
/// Minimal <see cref="IAlteration"/> used to lock save/load of serialized plan alterations.
/// </summary>
public sealed class TestAlteration : IAlteration
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
}
