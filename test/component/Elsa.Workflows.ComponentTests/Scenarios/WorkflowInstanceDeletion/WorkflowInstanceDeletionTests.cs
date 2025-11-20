using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion;

/// <summary>
/// Tests that workflow instances can be deleted reliably, even when they are running or being persisted.
/// This addresses issue #7077 where in-memory instances could be written back to the DB after deletion.
/// </summary>
public class WorkflowInstanceDeletionTests(App app) : AppComponentTest(app)
{
    private const string WorkflowStartedSignal = "workflow-started";

    private IWorkflowRuntime WorkflowRuntime => Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    private SignalManager SignalManager => Scope.ServiceProvider.GetRequiredService<SignalManager>();
    private IWorkflowInstanceStore WorkflowInstanceStore => Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

    private async Task<string> StartRunningWorkflowAsync()
    {
        var workflowClient = await WorkflowRuntime.CreateClientAsync();
        var runTask = workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SuspendingWorkflow.DefinitionId, VersionOptions.Published)
        });

        // Wait for the workflow to signal it has started
        return await SignalManager.WaitAsync<string>(WorkflowStartedSignal);
    }

    private async Task AssertWorkflowInstanceDeletedAsync(string workflowInstanceId)
    {
        var instance = await WorkflowInstanceStore.FindAsync(new() { Id = workflowInstanceId });
        Assert.Null(instance);
    }

    [Fact(DisplayName = "Delete running workflow instance should cancel and remove it")]
    public async Task DeleteRunningWorkflowInstance_ShouldCancelAndRemove()
    {
        // Arrange - Start a workflow that will be running in memory
        var workflowInstanceId = await StartRunningWorkflowAsync();

        // Act - Delete the running workflow instance using the API
        var deleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await deleteClient.DeleteAsync(workflowInstanceId);

        // Assert - Instance should be deleted
        await AssertWorkflowInstanceDeletedAsync(workflowInstanceId);
    }

    [Fact(DisplayName = "Delete finished workflow instance should remove it")]
    public async Task DeleteFinishedWorkflowInstance_ShouldRemove()
    {
        // Arrange - Create and run a workflow that completes immediately
        var workflowClient = await WorkflowRuntime.CreateClientAsync();
        var runResponse = await workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(CompletingWorkflow.DefinitionId, VersionOptions.Published)
        });
        var workflowInstanceId = runResponse.WorkflowInstanceId;

        // Verify the workflow finished
        var finishedInstance = await WorkflowInstanceStore.FindAsync(new() { Id = workflowInstanceId });
        Assert.NotNull(finishedInstance);
        Assert.Equal(WorkflowStatus.Finished, finishedInstance.WorkflowState.Status);

        // Act - Delete the finished workflow instance
        var deleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await deleteClient.DeleteAsync(workflowInstanceId);

        // Assert - Instance should be deleted
        await AssertWorkflowInstanceDeletedAsync(workflowInstanceId);
    }

    [Fact(DisplayName = "Delete non-existent workflow instance should throw exception")]
    public async Task DeleteNonExistentWorkflowInstance_ShouldThrowException()
    {
        // Arrange - Use a non-existent workflow instance ID
        var nonExistentId = "non-existent-workflow-instance-id";

        // Act & Assert - Should throw an ApiException for non-existent instance (404 Not Found)
        var deleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await Assert.ThrowsAsync<Refit.ApiException>(async () => await deleteClient.DeleteAsync(nonExistentId));
    }

    [Fact(DisplayName = "Bulk delete mixed running and finished instances should delete all")]
    public async Task BulkDeleteMixedInstances_ShouldDeleteAll()
    {
        // Arrange - Create 2 running and 2 finished workflow instances
        var workflowClient = await WorkflowRuntime.CreateClientAsync();

        // Start 2 running instances
        var runningId1 = await StartRunningWorkflowAsync();
        var runningId2 = await StartRunningWorkflowAsync();

        // Start 2 finished instances
        var finishedResult1 = await workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(CompletingWorkflow.DefinitionId, VersionOptions.Published)
        });
        var finishedResult2 = await workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(CompletingWorkflow.DefinitionId, VersionOptions.Published)
        });

        var allInstanceIds = new[] { runningId1, runningId2, finishedResult1.WorkflowInstanceId, finishedResult2.WorkflowInstanceId };

        // Act - Bulk delete all instances
        var bulkDeleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await bulkDeleteClient.BulkDeleteAsync(new() { Ids = allInstanceIds }, CancellationToken.None);

        // Assert - All instances should be deleted
        foreach (var instanceId in allInstanceIds)
        {
            await AssertWorkflowInstanceDeletedAsync(instanceId);
        }
    }

    [Fact(DisplayName = "Delete workflow instance should not be resurrected by persistence")]
    public async Task DeleteWorkflowInstance_ShouldNotBeResurrectedByPersistence()
    {
        // This test addresses the core issue in #7077: ensuring that a deleted workflow instance
        // is not written back to the database by the persistence mechanism while it's still executing.
        //
        // The race condition occurs when:
        // 1. Workflow starts executing (stays in memory)
        // 2. Delete request comes in
        // 3. Without the fix: deletion happens, but workflow continues and persists itself again
        // 4. With the fix: deletion blocks until workflow completes, using distributed locking

        // Arrange - Start a workflow that will be running in memory
        var workflowInstanceId = await StartRunningWorkflowAsync();

        // Act - Attempt to delete using the runtime client while workflow is executing
        // With the fix, this will block until the workflow completes (about 3 seconds)
        var instanceClient = await WorkflowRuntime.CreateClientAsync(workflowInstanceId);
        var deleted = await instanceClient.DeleteAsync();

        Assert.True(deleted);

        // Assert - Instance should be deleted and NOT resurrected
        await AssertWorkflowInstanceDeletedAsync(workflowInstanceId);
    }
}
