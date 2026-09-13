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

    public static async Task<AlterationStoreScenario> CreateSqliteAsync()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-alterations-conformance-{Guid.NewGuid():N}.db");
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        ServiceProvider? services = null;
        IServiceScope? scope = null;

        try
        {
            var migrationsAssembly = typeof(AlterationsDbContextFactories).Assembly;
            services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .AddSingleton<IAlterationSerializer, ConformanceAlterationSerializer>()
                .Configure<TenantsOptions>(options => options.IsEnabled = true)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<AlterationsElsaDbContext>((_, builder) =>
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={databasePath};Default Timeout=30"))
                .Decorate<IDbContextFactory<AlterationsElsaDbContext>, TenantAwareDbContextFactory<AlterationsElsaDbContext>>()
                .AddScoped<EntityStore<AlterationsElsaDbContext, AlterationPlan>>()
                .AddScoped<EntityStore<AlterationsElsaDbContext, AlterationJob>>()
                .AddScoped<EFCoreAlterationPlanStore>()
                .AddScoped<EFCoreAlterationJobStore>()
                .BuildServiceProvider();

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
                    SqliteConnection.ClearAllPools();
                    File.Delete(databasePath);
                });
        }
        catch
        {
            scope?.Dispose();
            if (services is not null)
                await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
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
/// Minimal <see cref="IAlteration"/> used to lock save/load of serialized plan alterations.
/// </summary>
public sealed class TestAlteration : IAlteration
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
}
