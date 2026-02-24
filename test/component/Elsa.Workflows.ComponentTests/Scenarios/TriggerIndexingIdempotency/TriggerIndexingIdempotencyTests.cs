using Elsa.Http;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.TriggerIndexingIdempotency;

/// <summary>
/// Tests that trigger indexing is idempotent: re-indexing an unchanged workflow should
/// not produce any trigger changes (no delete-reinsert cycle).
///
/// The WorkflowTriggerEqualityComparer must use consistent serialization settings
/// (matching IPayloadSerializer's camelCase convention) so that freshly computed triggers
/// and DB-loaded triggers are correctly recognized as equal. Without this, the diff
/// reports all triggers as changed on every indexing call, causing unnecessary churn
/// and creating a window for unique constraint violations in multi-pod environments.
/// </summary>
public class TriggerIndexingIdempotencyTests(App app) : AppComponentTest(app)
{
    [Fact(DisplayName = "Re-indexing an unchanged workflow should not produce any trigger changes")]
    public async Task ReIndexingUnchangedWorkflow_ShouldBeIdempotent()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var definition = await SaveWorkflowDefinitionAsync(workflow);
        var indexer = Scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();

        // Act: First indexing — triggers are inserted.
        var firstResult = await indexer.IndexTriggersAsync(definition);
        Assert.Single(firstResult.AddedTriggers);
        Assert.Empty(firstResult.RemovedTriggers);

        // Act: Second indexing — triggers already exist in the DB.
        var secondResult = await indexer.IndexTriggersAsync(definition);

        // Assert: The second indexing should be a no-op.
        Assert.Empty(secondResult.AddedTriggers);
        Assert.Empty(secondResult.RemovedTriggers);
        Assert.Single(secondResult.UnchangedTriggers);
    }

    [Fact(DisplayName = "Re-indexing should preserve trigger IDs when nothing changed")]
    public async Task ReIndexingUnchangedWorkflow_ShouldPreserveTriggerIds()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var definition = await SaveWorkflowDefinitionAsync(workflow);
        var indexer = Scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();

        // Act: Index twice.
        await indexer.IndexTriggersAsync(definition);
        var originalTriggerId = (await GetTriggersAsync(workflow.Identity.DefinitionId)).Single().Id;

        await indexer.IndexTriggersAsync(definition);
        var triggerIdAfterSecondIndex = (await GetTriggersAsync(workflow.Identity.DefinitionId)).Single().Id;

        // Assert: The trigger ID should be preserved — no delete-reinsert happened.
        Assert.Equal(originalTriggerId, triggerIdAfterSecondIndex);
    }

    private static Workflow CreateTestWorkflow()
    {
        var definitionId = Guid.NewGuid().ToString();
        var workflowId = Guid.NewGuid().ToString();

        return new()
        {
            Identity = new(definitionId, 1, workflowId),
            Root = new Sequence
            {
                Activities =
                {
                    new HttpEndpoint
                    {
                        Path = new($"/blob-import-test/{Guid.NewGuid()}"),
                        SupportedMethods = new(["GET"]),
                        CanStartWorkflow = true
                    }
                }
            },
            Publication = new(IsLatest: true, IsPublished: true)
        };
    }

    private async Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(Workflow workflow)
    {
        var workflowDefinitionStore = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var activitySerializer = Scope.ServiceProvider.GetRequiredService<IActivitySerializer>();

        var definition = new WorkflowDefinition
        {
            Id = workflow.Identity.Id,
            DefinitionId = workflow.Identity.DefinitionId,
            Version = workflow.Identity.Version,
            IsLatest = true,
            IsPublished = true,
            Name = "Blob Import Test Workflow",
            Description = "Simulates a workflow imported from blob storage",
            MaterializerName = "Json",
            StringData = activitySerializer.Serialize(workflow.Root)
        };

        await workflowDefinitionStore.SaveAsync(definition);
        return definition;
    }

    private async Task<List<StoredTrigger>> GetTriggersAsync(string workflowDefinitionId)
    {
        var triggerStore = Scope.ServiceProvider.GetRequiredService<ITriggerStore>();
        return (await triggerStore.FindManyAsync(new() { WorkflowDefinitionId = workflowDefinitionId })).ToList();
    }
}

