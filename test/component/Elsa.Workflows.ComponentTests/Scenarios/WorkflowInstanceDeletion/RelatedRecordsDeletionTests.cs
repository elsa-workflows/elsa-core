using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion;

/// <summary>
/// Tests that verify workflow instance deletion also deletes all related records
/// (execution logs, activity execution records, bookmarks, etc.)
/// </summary>
public class RelatedRecordsDeletionTests(App app) : AppComponentTest(app)
{
    private readonly HashSet<string> _createdWorkflowInstanceIds = new(StringComparer.Ordinal);

    private IWorkflowRuntime WorkflowRuntime => Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    private IWorkflowInstanceStore WorkflowInstanceStore => Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
    private IBookmarkStore BookmarkStore => Scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
    private IActivityExecutionStore ActivityExecutionStore => Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
    private IWorkflowExecutionLogStore WorkflowExecutionLogStore => Scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>();
    private IExecutionCycleRegistry ExecutionCycleRegistry => Scope.ServiceProvider.GetRequiredService<IExecutionCycleRegistry>();

    [Test]
    [DisplayName("Delete via API endpoint should remove all related records")]
    public async Task DeleteViaApiEndpoint_ShouldRemoveAllRelatedRecords()
    {
        // Arrange
        var workflowInstanceId = await CreateWorkflowWithRelatedRecordsAsync();
        await AssertRelatedRecordsExistAsync(workflowInstanceId);

        // Act
        var deleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await deleteClient.DeleteAsync(workflowInstanceId);

        // Assert
        await AssertAllRecordsDeletedAsync(workflowInstanceId);
    }

    [Test]
    [DisplayName("Bulk delete via API endpoint should remove all related records")]
    public async Task BulkDeleteViaApiEndpoint_ShouldRemoveAllRelatedRecords()
    {
        // Arrange - Create 2 workflow instances
        var workflowInstanceId1 = await CreateWorkflowWithRelatedRecordsAsync();
        var workflowInstanceId2 = await CreateWorkflowWithRelatedRecordsAsync();

        await AssertRelatedRecordsExistAsync(workflowInstanceId1);
        await AssertRelatedRecordsExistAsync(workflowInstanceId2);

        var allInstanceIds = new[]
        {
            workflowInstanceId1, workflowInstanceId2
        };

        // Act
        var bulkDeleteClient = WorkflowServer.CreateApiClient<IWorkflowInstancesApi>();
        await bulkDeleteClient.BulkDeleteAsync(new()
        {
            Ids = allInstanceIds
        }, CancellationToken.None);

        // Assert
        foreach (var instanceId in allInstanceIds)
        {
            await AssertAllRecordsDeletedAsync(instanceId);
        }
    }

    [Test]
    [DisplayName("Delete via runtime client should remove all related records")]
    public async Task DeleteViaRuntimeClient_ShouldRemoveAllRelatedRecords()
    {
        // Arrange
        var workflowInstanceId = await CreateWorkflowWithRelatedRecordsAsync();
        await AssertRelatedRecordsExistAsync(workflowInstanceId);

        // Act
        var instanceClient = await WorkflowRuntime.CreateClientAsync(workflowInstanceId);
        var deleted = await instanceClient.DeleteAsync();

        await Assert.That(deleted).IsTrue();

        // Assert
        await AssertAllRecordsDeletedAsync(workflowInstanceId);
    }
    
    private async Task<string> CreateWorkflowWithRelatedRecordsAsync()
    {
        var workflowClient = await WorkflowRuntime.CreateClientAsync();
        var runResponse = await workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                WorkflowWithRelatedRecords.DefinitionId,
                VersionOptions.Published)
        });
        _createdWorkflowInstanceIds.Add(runResponse.WorkflowInstanceId);
        return runResponse.WorkflowInstanceId;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        // Cancelling a running workflow uses a replacement execution pipeline. The deletion has
        // already removed the instance and its related records, so that pipeline has no later
        // commit at which its execution-cycle handle can be released. Dispose only handles for
        // instances this test created and has proved absent, before host drain starts.
        foreach (var handle in ExecutionCycleRegistry.ListActiveCycles()
                     .Where(x => _createdWorkflowInstanceIds.Contains(x.WorkflowInstanceId)))
        {
            var workflowInstance = await WorkflowInstanceStore.FindAsync(new() { Id = handle.WorkflowInstanceId });
            if (workflowInstance is null)
                handle.Dispose();
        }
    }

    private async Task AssertRelatedRecordsExistAsync(string workflowInstanceId)
    {
        var bookmarks = await BookmarkStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        var activityExecutions = await ActivityExecutionStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        var executionLogs = await WorkflowExecutionLogStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        }, PageArgs.All);

        await Assert.That(bookmarks).IsNotEmpty();
        await Assert.That(activityExecutions).IsNotEmpty();
        await Assert.That(executionLogs.Items).IsNotEmpty();
    }

    private async Task AssertAllRecordsDeletedAsync(string workflowInstanceId)
    {
        var workflowInstance = await WorkflowInstanceStore.FindAsync(new()
        {
            Id = workflowInstanceId
        });
        await Assert.That(workflowInstance).IsNull();

        var remainingBookmarks = await BookmarkStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        await Assert.That(remainingBookmarks).IsEmpty();

        var remainingActivityExecutions = await ActivityExecutionStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        await Assert.That(remainingActivityExecutions).IsEmpty();

        var remainingExecutionLogs = await WorkflowExecutionLogStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        }, PageArgs.All);
        await Assert.That(remainingExecutionLogs.Items).IsEmpty();
    }
}
