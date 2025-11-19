using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDeletion.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDeletion;

/// <summary>
/// Tests for workflow deletion to ensure instances don't resurrect after deletion.
/// </summary>
public class WorkflowDeletionTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowInstanceManager _workflowInstanceManager;
    private readonly IBookmarkStore _bookmarkStore;

    public WorkflowDeletionTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .AddWorkflow<SuspendedWorkflowForDeletion>()
            .Build();

        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        _workflowInstanceManager = _services.GetRequiredService<IWorkflowInstanceManager>();
        _bookmarkStore = _services.GetRequiredService<IBookmarkStore>();
    }

    [Fact(DisplayName = "Deleting a suspended workflow marks it as deleted")]
    public async Task DeleteSuspendedWorkflow_MarksAsDeleted()
    {
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SuspendedWorkflowForDeletion);
        
        // Create and run a workflow that suspends
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        var runResponse = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        
        Assert.Equal(WorkflowStatus.Running, runResponse.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, runResponse.SubStatus);

        var workflowInstanceId = workflowClient.WorkflowInstanceId;

        // Delete the workflow
        await workflowClient.DeleteAsync();

        // Verify instance is marked as deleted
        var workflowInstance = await _workflowInstanceManager.FindByIdAsync(workflowInstanceId);
        Assert.NotNull(workflowInstance);
        Assert.Equal(WorkflowStatus.Finished, workflowInstance.Status);
        Assert.Equal(WorkflowSubStatus.Deleted, workflowInstance.SubStatus);
    }

    [Fact(DisplayName = "Deleting a workflow removes its bookmarks")]
    public async Task DeleteWorkflow_RemovesBookmarks()
    {
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SuspendedWorkflowForDeletion);
        
        // Create and run a workflow that suspends (creates bookmarks)
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        var workflowInstanceId = workflowClient.WorkflowInstanceId;

        // Verify bookmarks exist before deletion
        var bookmarksBeforeDeletion = await _bookmarkStore.FindManyAsync(
            new BookmarkFilter { WorkflowInstanceId = workflowInstanceId });
        Assert.NotEmpty(bookmarksBeforeDeletion);

        // Delete the workflow
        await workflowClient.DeleteAsync();

        // Verify bookmarks are removed
        var bookmarksAfterDeletion = await _bookmarkStore.FindManyAsync(
            new BookmarkFilter { WorkflowInstanceId = workflowInstanceId });
        Assert.Empty(bookmarksAfterDeletion);
    }

    [Fact(DisplayName = "Attempting to run a deleted workflow does nothing")]
    public async Task RunDeletedWorkflow_DoesNotExecute()
    {
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SuspendedWorkflowForDeletion);
        
        // Create and run a workflow that suspends
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        // Delete the workflow
        await workflowClient.DeleteAsync();

        // Try to run the deleted workflow
        var runResponse = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        // Verify it returns deleted status without executing
        Assert.Equal(WorkflowStatus.Finished, runResponse.Status);
        Assert.Equal(WorkflowSubStatus.Deleted, runResponse.SubStatus);
    }

    [Fact(DisplayName = "Deleted workflow does not persist on subsequent save attempts")]
    public async Task DeletedWorkflow_DoesNotPersist()
    {
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SuspendedWorkflowForDeletion);
        
        // Create and run a workflow
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        var workflowInstanceId = workflowClient.WorkflowInstanceId;

        // Delete the workflow
        await workflowClient.DeleteAsync();

        // Get the deleted instance
        var deletedInstance = await _workflowInstanceManager.FindByIdAsync(workflowInstanceId);
        Assert.NotNull(deletedInstance);
        Assert.Equal(WorkflowSubStatus.Deleted, deletedInstance.SubStatus);

        // Try to save the deleted instance (simulating in-memory workflow trying to persist)
        await _workflowInstanceManager.SaveAsync(deletedInstance);

        // Verify it's still marked as deleted and wasn't resurrected with a different status
        var instanceAfterSave = await _workflowInstanceManager.FindByIdAsync(workflowInstanceId);
        Assert.NotNull(instanceAfterSave);
        Assert.Equal(WorkflowSubStatus.Deleted, instanceAfterSave.SubStatus);
    }
}
