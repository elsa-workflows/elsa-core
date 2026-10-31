using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExecuteWorkflowCancellation;

/// <summary>
/// What happens to a parent whose <c>ExecuteWorkflow</c> activity is waiting for a child workflow that is cancelled.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<ExecuteAndWaitWorkflow>())
{
    [Fact(DisplayName = "Cancelling a waited-for child through the cancellation service resumes its parent")]
    public async Task CancellingTheWaitedForChild_ResumesTheParent()
    {
        // Arrange: the parent is waiting for its suspended child.
        var parentId = await StartParentAsync(nameof(ExecuteAndWaitWorkflow));
        var childId = (await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended)).Id;

        // Act: cancel the child, the way the cancellation API does.
        Assert.True(await Services.GetRequiredService<IWorkflowCancellationService>().CancelWorkflowAsync(childId));
        var child = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished);
        Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);

        // The test host runs no bookmark queue worker, so deliver the resume the child's cancellation queued for its parent.
        await Services.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();

        // Assert: the parent's ExecuteWorkflow completed, as it does for a child that finishes, so the parent finished.
        var parent = await ExportStateAsync(parentId);
        Assert.Equal(WorkflowStatus.Finished, parent.Status);
        Assert.Equal(WorkflowSubStatus.Finished, parent.SubStatus);
        Assert.Empty(parent.Incidents);
    }
}
