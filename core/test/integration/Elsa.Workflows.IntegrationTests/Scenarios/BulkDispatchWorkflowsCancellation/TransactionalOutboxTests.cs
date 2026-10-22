using Elsa.KeyValues.Features;
using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens to bulk-dispatched child workflows when the waiting activity is cancelled, with the transactional
/// outbox on, so each child's properties including WaitingActivityInstanceId travel through outbox save and delivery.
/// </summary>
public class TransactionalOutboxTests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<FireAndForgetChildWorkflow>()
    .AddWorkflow<SecondBulkChildWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitBesideSiblingDispatchesWorkflow>()
    .ConfigureElsa(elsa => elsa.Configure<KeyValueFeature>())
    .ConfigureServices(services => services.Configure<WorkflowDispatcherOptions>(options =>
    {
        options.UseTransactionalOutbox = true;
        options.ProcessOutboxAfterCommit = false;
    })))
{
    [Fact(DisplayName = "With the outbox on, cancelling a waiting BulkDispatchWorkflows cancels the children it dispatched after they are delivered")]
    public async Task CancellingTheWaitingBulkDispatchActivity_AfterOutboxDelivery_CancelsTheDeliveredChildren()
    {
        const int waitedChildCount = BulkDispatchAndWaitBesideSiblingDispatchesWorkflow.WaitedChildCount;

        // Arrange: the parent commits the children to the outbox. They are not created until the outbox is delivered.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitBesideSiblingDispatchesWorkflow));
        Assert.NotEmpty(await Services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync());

        await DeliverOutboxAsync();
        Assert.Empty(await Services.GetRequiredService<IWorkflowDispatchOutboxStore>().FindManyAsync());

        await WaitForChildAsync(parentId, nameof(FireAndForgetChildWorkflow), IsSuspended);
        await WaitForChildAsync(parentId, nameof(SecondBulkChildWorkflow), IsSuspended);
        var waitedChildren = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsSuspended);
        var waitedChildIds = waitedChildren.Select(x => x.Id).Order().ToList();
        Assert.All(waitedChildren, child => Assert.True(child.Properties.ContainsKey("WaitingActivityInstanceId")));

        // Reload the parent after delivery so cancel runs against the committed, post-outbox state.
        await ExportStateAsync(parentId);

        // Act: cancel the first bulk activity. The second one keeps the parent waiting.
        var resumed = await ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
        Assert.Equal(WorkflowSubStatus.Suspended, resumed.SubStatus);

        // Assert: exact cancellation targets — only the waited-for children of the cancelled activity.
        Assert.Equal(waitedChildIds, CancellationTargets.Order());
        var cancelledChildren = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsFinished);
        Assert.All(cancelledChildren, child => Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus));
    }
}
