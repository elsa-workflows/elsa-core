using CShells.DependencyInjection;
using CShells.Lifecycle;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Runtime;
using Elsa.Tenants.ShellFeatures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.ShellFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.TenantAgnosticTriggers;

/// <summary>
/// On a shell host, the persistence feature registers its trigger store after the runtime feature. A tenant that overrides a
/// tenant-agnostic definition must still not see its shared triggers there.
/// </summary>
public class ShellHostTests : IAsyncLifetime
{
    private const string DefinitionId = "shared-definition";
    private readonly string _databasePath = Path.Join(Path.GetTempPath(), $"elsa-shell-shared-triggers-{Guid.NewGuid():N}.db");
    private ServiceProvider _rootServices = null!;
    private IShell _shell = null!;

    public async Task InitializeAsync()
    {
        var connectionString = $"Data Source={_databasePath};Pooling=False";
        var services = new ServiceCollection().AddLogging();
        services.AddCShells(shells => shells
            .WithAssemblies(typeof(WorkflowRuntimeFeature).Assembly, typeof(WorkflowManagementFeature).Assembly, typeof(SqliteWorkflowRuntimePersistenceShellFeature).Assembly, typeof(TenantsFeature).Assembly)
            .AddShell("Default", shell => shell
                .WithFeature<TenantsFeature>()
                .WithFeature<WorkflowManagementFeature>()
                .WithFeature<CachingWorkflowDefinitionsFeature>()
                .WithFeature<WorkflowRuntimeFeature>()
                .WithFeature<CachingWorkflowRuntimeFeature>()
                .WithFeature<SqliteWorkflowRuntimePersistenceShellFeature>(feature => feature.ConnectionString = connectionString)));
        _rootServices = services.BuildServiceProvider();
        _shell = await _rootServices.GetRequiredService<IShellRegistry>().GetOrActivateAsync("Default");
    }

    public async Task DisposeAsync()
    {
        await _rootServices.DisposeAsync();
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [Fact(DisplayName = "On a shell host with EF Core runtime persistence, an overriding tenant does not see the shared trigger")]
    public async Task OverridingTenantDoesNotSeeSharedTrigger()
    {
        await InTenantAsync(Tenant.AgnosticTenantId, async sp =>
        {
            using var definitionGrant = SharedTenantWriteScope.Enter<WorkflowDefinition>();
            using var triggerGrant = SharedTenantWriteScope.Enter<StoredTrigger>();
            await sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(Definition("shared-v1", Tenant.AgnosticTenantId));
            await sp.GetRequiredService<ITriggerStore>().SaveAsync(new StoredTrigger
            {
                Id = "shared-trigger",
                TenantId = Tenant.AgnosticTenantId,
                WorkflowDefinitionId = DefinitionId,
                WorkflowDefinitionVersionId = "shared-v1",
                Name = "Elsa.Event",
                ActivityId = "event",
                Hash = "shared-hash"
            });
        });
        await InTenantAsync("a", sp => sp.GetRequiredService<IWorkflowDefinitionStore>().SaveAsync(Definition("a-v1", "a")));

        var seenByA = await FindAsync("a");
        var seenByB = await FindAsync("b");

        Assert.Empty(seenByA);
        Assert.Equal(["shared-trigger"], seenByB.Select(x => x.Id));
    }

    private async Task<List<StoredTrigger>> FindAsync(string tenantId)
    {
        var triggers = new List<StoredTrigger>();
        await InTenantAsync(tenantId, async sp => triggers.AddRange(await sp.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { Hash = "shared-hash" })));
        return triggers;
    }

    private async Task InTenantAsync(string tenantId, Func<IServiceProvider, Task> action)
    {
        await using var scope = _shell.ServiceProvider.GetRequiredService<ITenantScopeFactory>().CreateScope(new Tenant { Id = tenantId, Name = tenantId });
        await action(scope.ServiceProvider);
    }

    private static WorkflowDefinition Definition(string id, string tenantId) => new()
    {
        Id = id,
        DefinitionId = DefinitionId,
        Version = 1,
        TenantId = tenantId,
        IsLatest = true,
        IsPublished = true,
        MaterializerName = "Json"
    };
}
