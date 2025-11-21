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
    private IWorkflowRuntime WorkflowRuntime => Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    private IWorkflowInstanceStore WorkflowInstanceStore => Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
    private IBookmarkStore BookmarkStore => Scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
    private IActivityExecutionStore ActivityExecutionStore => Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
    private IWorkflowExecutionLogStore WorkflowExecutionLogStore => Scope.ServiceProvider.GetRequiredService<IWorkflowExecutionLogStore>();

    [Fact(DisplayName = "Delete via API endpoint should remove all related records")]
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

    [Fact(DisplayName = "Bulk delete via API endpoint should remove all related records")]
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

    [Fact(DisplayName = "Delete via runtime client should remove all related records")]
    public async Task DeleteViaRuntimeClient_ShouldRemoveAllRelatedRecords()
    {
        // Arrange
        var workflowInstanceId = await CreateWorkflowWithRelatedRecordsAsync();
        await AssertRelatedRecordsExistAsync(workflowInstanceId);

        // Act
        var instanceClient = await WorkflowRuntime.CreateClientAsync(workflowInstanceId);
        var deleted = await instanceClient.DeleteAsync();

        Assert.True(deleted);

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
        return runResponse.WorkflowInstanceId;
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

        Assert.NotEmpty(bookmarks);
        Assert.NotEmpty(activityExecutions);
        Assert.NotEmpty(executionLogs.Items);
    }

    private async Task AssertAllRecordsDeletedAsync(string workflowInstanceId)
    {
        var workflowInstance = await WorkflowInstanceStore.FindAsync(new()
        {
            Id = workflowInstanceId
        });
        Assert.Null(workflowInstance);

        var remainingBookmarks = await BookmarkStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        Assert.Empty(remainingBookmarks);

        var remainingActivityExecutions = await ActivityExecutionStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        Assert.Empty(remainingActivityExecutions);

        var remainingExecutionLogs = await WorkflowExecutionLogStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        }, PageArgs.All);
        Assert.Empty(remainingExecutionLogs.Items);
    }
}