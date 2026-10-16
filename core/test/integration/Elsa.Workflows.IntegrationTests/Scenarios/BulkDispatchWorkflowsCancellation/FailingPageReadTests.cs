using Elsa.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens when the cancel cascade cannot read one page of children and there are more pages after it.
/// </summary>
public class FailingPageReadTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
    .ConfigureElsa(elsa => elsa.UseWorkflowManagement(management => management.UseWorkflowInstances(instances =>
    {
        var inner = instances.WorkflowInstanceStore;
        instances.WorkflowInstanceStore = sp => new FailingOncePagedReadWorkflowInstanceStore(inner(sp));
    }))))
{
    [Fact(DisplayName = "A failed first-page read still cancels children on the later pages")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenTheFirstPageReadFails_CancelsLaterPages()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;
        const int pageSize = 100;

        // Arrange: more than one page of children, ordered the same way the cancel cascade reads them.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();
        var laterPageIds = childIds.Skip(pageSize).ToList();
        Assert.NotEmpty(laterPageIds);

        // Act: the first page read throws once. The parent must still finish, and later pages must still be cancelled.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Equal(laterPageIds, CancellationTargets.Order());
    }
}
