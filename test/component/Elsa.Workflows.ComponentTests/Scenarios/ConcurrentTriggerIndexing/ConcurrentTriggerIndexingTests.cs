using Elsa.Http;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ConcurrentTriggerIndexing;

/// <summary>
/// Tests for reproducing and preventing duplicate trigger registration in multi-engine scenarios.
/// Simulates the race condition that occurs when multiple engines load and index
/// workflows from blob storage simultaneously.
/// </summary>
public class ConcurrentTriggerIndexingTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Concurrent trigger indexing should not create duplicates")]
    [InlineData(10, 0, false, "Synchronized start with 10 operations")]
    [InlineData(10, 5, false, "Staggered start with random delays")]
    [InlineData(3, 0, true, "Multiple rounds of concurrent indexing")]
    public async Task ConcurrentIndexing_ShouldNotCreateDuplicates(
        int concurrentOperations,
        int maxRandomDelayMs,
        bool multipleRounds,
        string scenario)
    {
        // Arrange
        var (workflow, workflowDefinition) = await CreateAndSaveTestWorkflowAsync();

        // Act
        var rounds = multipleRounds ? 3 : 1;
        for (var round = 0; round < rounds; round++)
        {
            await ExecuteConcurrentIndexingAsync(workflowDefinition, concurrentOperations, maxRandomDelayMs);
        }

        // Assert
        await AssertSingleTriggerExistsAsync(workflow.Identity.DefinitionId, scenario);
    }

    [Fact(DisplayName = "Concurrent workflow refreshes should not create duplicates")]
    public async Task ConcurrentWorkflowRefresh_ShouldNotCreateDuplicates()
    {
        // Arrange
        var (workflow, _) = await CreateAndSaveTestWorkflowAsync();

        // Act: Simulate concurrent refresh calls from different engines (via API or file watcher)
        var refreshTasks = AllPods.Select(pod => Task.Run(async () =>
        {
            using var scope = pod.Services.CreateScope();
            var refresher = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsRefresher>();
            return await refresher.RefreshWorkflowDefinitionsAsync(new()
            {
                DefinitionIds = [workflow.Identity.DefinitionId]
            }, CancellationToken.None);
        })).ToArray();

        await Task.WhenAll(refreshTasks);

        // Assert
        await AssertSingleTriggerExistsAsync(workflow.Identity.DefinitionId);
    }

    [Fact(DisplayName = "Attempting to create duplicate triggers should fail with unique constraint violation")]
    public async Task ManuallyCreatedDuplicates_ShouldViolateUniqueConstraint()
    {
        // Arrange: This test verifies the database unique constraint protection layer
        var (workflow, workflowDefinition) = await CreateAndSaveTestWorkflowAsync();
        var triggerStore = GetTriggerStore();

        // Index the workflow first
        var indexer = Scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();
        await indexer.IndexTriggersAsync(workflowDefinition);

        // Act & Assert: Attempting to create a duplicate should throw DbUpdateException
        var existingTriggers = await GetTriggersAsync(workflow.Identity.DefinitionId);
        var duplicateTrigger = CreateDuplicateTrigger(existingTriggers[0]);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await triggerStore.SaveAsync(duplicateTrigger));

        // Verify it's specifically a unique constraint violation
        var message = exception.InnerException?.Message ?? exception.Message;
        Assert.True(
            message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("23505", StringComparison.OrdinalIgnoreCase),
            $"Expected unique constraint violation, but got: {message}");
    }

    private async Task<(Workflow workflow, WorkflowDefinition definition)> CreateAndSaveTestWorkflowAsync()
    {
        var workflow = CreateTestWorkflow();
        var definition = await SaveWorkflowDefinitionAsync(workflow);
        return (workflow, definition);
    }

    private async Task ExecuteConcurrentIndexingAsync(
        WorkflowDefinition workflowDefinition,
        int concurrentOperations,
        int maxRandomDelayMs)
    {
        var startBarrier = new TaskCompletionSource();

        var indexingTasks = Enumerable.Range(0, concurrentOperations)
            .Select(i => CreateIndexingTask(workflowDefinition, startBarrier, i, maxRandomDelayMs))
            .ToArray();

        // Small delay to ensure all tasks are waiting
        await Task.Delay(50);

        // Release all tasks simultaneously
        startBarrier.SetResult();

        await Task.WhenAll(indexingTasks);
    }

    private Task CreateIndexingTask(
        WorkflowDefinition workflowDefinition,
        TaskCompletionSource startBarrier,
        int taskIndex,
        int maxRandomDelayMs)
    {
        return Task.Run(async () =>
        {
            await startBarrier.Task;

            // Add random delay if specified (simulates real-world timing variations)
            if (maxRandomDelayMs > 0)
                await Task.Delay(Random.Shared.Next(1, maxRandomDelayMs + 1));

            var pod = GetPodByIndex(taskIndex);
            using var scope = pod.Services.CreateScope();
            var indexer = scope.ServiceProvider.GetRequiredService<ITriggerIndexer>();
            return await indexer.IndexTriggersAsync(workflowDefinition);
        });
    }

    private WorkflowServer GetPodByIndex(int taskIndex)
    {
        return (taskIndex % AllPods.Length) switch
        {
            0 => Cluster.Pod1,
            1 => Cluster.Pod2,
            _ => Cluster.Pod3
        };
    }

    private WorkflowServer[] AllPods => [Cluster.Pod1, Cluster.Pod2, Cluster.Pod3];

    private async Task AssertSingleTriggerExistsAsync(string workflowDefinitionId, string? scenario = null)
    {
        var triggers = await GetTriggersAsync(workflowDefinitionId);
        var message = scenario != null
            ? $"Expected exactly 1 trigger for scenario: {scenario}, but found {triggers.Count}"
            : $"Expected exactly 1 trigger, but found {triggers.Count}";
        Assert.True(triggers.Count == 1, message);
    }

    private async Task<List<StoredTrigger>> GetTriggersAsync(string workflowDefinitionId)
    {
        var triggerStore = GetTriggerStore();
        return (await triggerStore.FindManyAsync(new()
        {
            WorkflowDefinitionId = workflowDefinitionId
        })).ToList();
    }

    private ITriggerStore GetTriggerStore() => Scope.ServiceProvider.GetRequiredService<ITriggerStore>();

    private static Workflow CreateTestWorkflow()
    {
        var httpEndpoint = new HttpEndpoint
        {
            Path = new($"/test-concurrent/{Guid.NewGuid()}"),
            SupportedMethods = new(["GET"]),
            CanStartWorkflow = true
        };

        var definitionId = Guid.NewGuid().ToString();
        var workflowId = Guid.NewGuid().ToString();

        return new()
        {
            Identity = new(definitionId, 1, workflowId),
            Root = new Sequence
            {
                Activities =
                {
                    httpEndpoint
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
            Name = "Test Concurrent Workflow",
            Description = "Workflow for testing concurrent trigger indexing",
            MaterializerName = "Json",
            StringData = activitySerializer.Serialize(workflow.Root)
        };

        await workflowDefinitionStore.SaveAsync(definition);
        return definition;
    }

    private static StoredTrigger CreateDuplicateTrigger(StoredTrigger original)
    {
        return new()
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowDefinitionId = original.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = original.WorkflowDefinitionVersionId,
            Name = original.Name,
            ActivityId = original.ActivityId,
            Hash = original.Hash,
            Payload = original.Payload
        };
    }
}