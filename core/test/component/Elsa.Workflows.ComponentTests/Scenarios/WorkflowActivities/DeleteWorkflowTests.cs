using System.Diagnostics;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class DeleteWorkflowTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task DeleteWorkflow()
    {
        EnsureWorkflowInRegistry(Scope, Workflows.DeleteWorkflow.Type);

        var workflowDefinitionManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        var deletedCount = await workflowDefinitionManager.DeleteByDefinitionIdAsync(Workflows.DeleteWorkflow.DefinitionId);
        Assert.True(deletedCount > 0, "Expected workflow definition to be deleted.");
        
        var store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var t1 = await store.FindAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = Workflows.DeleteWorkflow.DefinitionId
        });
        
        Assert.Null(t1);

        // Force a refresh of the activity registry to ensure it reflects the deletion
        var activityRegistry = Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var workflowDefinitionActivityProvider = Scope.ServiceProvider.GetRequiredService<WorkflowDefinitionActivityProvider>();
        await activityRegistry.RefreshDescriptorsAsync(workflowDefinitionActivityProvider);

        // Verify the workflow is removed from the registry
        WorkflowTypeDeletedFromRegistry(Scope, Workflows.DeleteWorkflow.Type);
    }

    [Fact]
    public async Task DeleteWorkflow_Clustered()
    {
        using var pod1Scope = Cluster.Pod1.Services.CreateScope();
        using var pod2Scope = Cluster.Pod2.Services.CreateScope();
        using var pod3Scope = Cluster.Pod3.Services.CreateScope();
        var registries = new[] { pod1Scope, pod2Scope, pod3Scope }
            .Select(scope => scope.ServiceProvider.GetRequiredService<IActivityRegistry>()).ToArray();
        var definitionId = $"DeleteWorkflowClustered{Guid.NewGuid():N}";
        var activityName = definitionId.Pascalize();
        var importer = pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionImporter>();
        var manager = pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        var store = pod1Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();

        try
        {
            var imported = await importer.ImportAsync(new SaveWorkflowDefinitionRequest
            {
                Model = new WorkflowDefinitionModel
                {
                    Name = activityName,
                    DefinitionId = definitionId,
                    Options = new WorkflowOptions { UsableAsActivity = true }
                },
                Publish = true
            });
            Assert.True(imported.Succeeded, string.Join(Environment.NewLine, imported.ValidationErrors.Select(error => error.Message)));
            await WaitForRegistryConvergenceAsync(registries, activityName, expectedPresent: true);

            var deletedCount = await manager.DeleteByDefinitionIdAsync(definitionId);
            Assert.Equal(1L, deletedCount);
            Assert.Null(await store.FindAsync(new WorkflowDefinitionFilter { DefinitionId = definitionId }));

            // Observe the actual generation/refresh path on every pod without forcing a refresh.
            await WaitForRegistryConvergenceAsync(registries, activityName, expectedPresent: false);
        }
        finally
        {
            await manager.DeleteByDefinitionIdAsync(definitionId);
        }
    }

    private static void EnsureWorkflowInRegistry(IServiceScope scope, string type)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(type);
        Assert.NotNull(descriptor);
    }

    private static void WorkflowTypeDeletedFromRegistry(IServiceScope scope, string type)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(type);

        Assert.Null(descriptor);
    }

    private static async Task WaitForRegistryConvergenceAsync(IActivityRegistry[] registries, string activityName, bool expectedPresent)
    {
        bool Converged() => registries.All(registry => (registry.Find(activityName) is not null) == expectedPresent);
        var elapsed = Stopwatch.StartNew();
        while (!Converged() && elapsed.Elapsed < TimeSpan.FromSeconds(30))
        {
            await Task.Delay(100);
        }

        Assert.True(Converged(), $"Expected workflow activity presence {expectedPresent} on all three pods.");
    }
}
