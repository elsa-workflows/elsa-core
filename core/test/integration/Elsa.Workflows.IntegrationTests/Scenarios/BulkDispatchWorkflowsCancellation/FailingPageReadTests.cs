using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens when the cancel cascade cannot read a page of children on the first try, or at all.
/// </summary>
public class FailingPageReadTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => Configure(builder))
{
    internal static TestApplicationBuilder Configure(TestApplicationBuilder builder, int failureCount = 1) => builder
        .AddWorkflow<BlockingChildWorkflow>()
        .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
        .ConfigureElsa(elsa => elsa.UseWorkflowManagement(management => management.UseWorkflowInstances(instances =>
        {
            var inner = instances.WorkflowInstanceStore;
            instances.WorkflowInstanceStore = sp => new FailingOncePagedReadWorkflowInstanceStore(inner(sp), failureCount);
        })));

    [Fact(DisplayName = "A one-time page-read failure is retried and no page is skipped")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenTheFirstPageReadFailsOnce_RetriesAndCancelsEveryPage()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: more than one page of children, ordered the same way the cancel cascade reads them.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: the first page read throws once, then succeeds on retry. No page is skipped.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Equal(childIds, CancellationTargets.Order());
    }
}

/// <summary>
/// What happens when every cancel-cascade page read fails, including the one retry, until the consecutive-failure cap.
/// </summary>
public class GivingUpPageReadTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder =>
    FailingPageReadTests.Configure(builder, failureCount: int.MaxValue))
{
    [Fact(DisplayName = "Paging gives up after three consecutive failed page reads")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenEveryPageReadFails_GivesUpAfterTheCap()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: children exist so the waiting activity has something to look for. Every cascade read will throw.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended);

        // Act: each page fails, is retried, then skipped, until the cap. The parent must still finish.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);

        Assert.Empty(CancellationTargets);
    }
}
