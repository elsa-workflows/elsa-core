using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Persistence.ConformanceTests;

/// <summary>
/// Holds one InMemory or EF/SQLite pair of Labels stores and the ambient tenant they read.
/// </summary>
public sealed class LabelStoreScenario(
    TestTenantAccessor tenantAccessor,
    ILabelStore labels,
    IWorkflowDefinitionLabelStore associations,
    IWorkflowDefinitionLabelQuery associationQuery,
    Func<Func<Task>, Task> assertUniquenessConflictAsync,
    Func<ValueTask> disposeAsync) : IAsyncDisposable
{
    public TestTenantAccessor TenantAccessor { get; } = tenantAccessor;
    public ILabelStore Labels { get; } = labels;
    public IWorkflowDefinitionLabelStore Associations { get; } = associations;
    public IWorkflowDefinitionLabelQuery AssociationQuery { get; } = associationQuery;

    public IDisposable UseTenant(string tenantId) =>
        TenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId
            ? Tenant.Default
            : new Tenant { Id = tenantId, Name = tenantId });

    public Task AssertUniquenessConflictAsync(Func<Task> operation) => assertUniquenessConflictAsync(operation);

    public ValueTask DisposeAsync() => disposeAsync();

    public static Task<LabelStoreScenario> CreateInMemoryAsync()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        var labels = new MemoryStore<Label>();
        var associations = new MemoryStore<WorkflowDefinitionLabel>();
        var labelStore = new InMemoryLabelStore(labels, associations, tenantAccessor);
        var associationStore = new InMemoryWorkflowDefinitionLabelStore(associations, tenantAccessor);

        return Task.FromResult(new LabelStoreScenario(
            tenantAccessor,
            labelStore,
            associationStore,
            associationStore,
            async operation => { await Assert.ThrowsExactlyAsync<InvalidOperationException>(operation); },
            () => ValueTask.CompletedTask));
    }

    public static async Task<LabelStoreScenario> CreateSqliteAsync()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-labels-conformance-{Guid.NewGuid():N}.db");
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        ServiceProvider? services = null;
        IServiceScope? scope = null;

        try
        {
            var migrationsAssembly = typeof(LabelsDbContextFactory).Assembly;
            services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .Configure<TenantsOptions>(options => options.IsEnabled = true)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<LabelsElsaDbContext>((_, builder) =>
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={databasePath};Default Timeout=30"))
                .Decorate<IDbContextFactory<LabelsElsaDbContext>, TenantAwareDbContextFactory<LabelsElsaDbContext>>()
                .AddScoped<EntityStore<LabelsElsaDbContext, Label>>()
                .AddScoped<EntityStore<LabelsElsaDbContext, WorkflowDefinitionLabel>>()
                .AddScoped<EFCoreLabelStore>()
                .AddScoped<EFCoreWorkflowDefinitionLabelStore>()
                .BuildServiceProvider();

            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<LabelsElsaDbContext>>().CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            scope = services.CreateScope();
            var scoped = scope.ServiceProvider;
            var associationStore = scoped.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>();

            return new(
                tenantAccessor,
                scoped.GetRequiredService<EFCoreLabelStore>(),
                associationStore,
                associationStore,
                AssertSqliteUniquenessConflictAsync,
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

    private static async Task AssertSqliteUniquenessConflictAsync(Func<Task> operation)
    {
        var exception = await CaptureExceptionAsync(operation);
        var sqliteException = exception switch
        {
            DbUpdateException { InnerException: SqliteException inner } => inner,
            SqliteException direct => direct,
            _ => throw new TUnit.Assertions.Exceptions.AssertionException($"Expected a SQLite uniqueness violation, received {exception?.GetType().FullName ?? "no exception"}.")
        };

        await Assert.That(sqliteException.SqliteErrorCode).IsEqualTo(19);
        await Assert.That(sqliteException.Message).Contains("UNIQUE constraint failed").WithComparison(StringComparison.Ordinal);
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
