using Elsa.Workflows;
using Elsa.Workflows.State;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// Catch events armed by an imported process and fired by real stimuli: an event-based gateway racing a message
/// against a timer, message and signal boundary events on a receive task, and signal-triggered event subprocesses.
/// </summary>
/// <remarks>
/// Every catch here is the activity the binder produces for it -- <c>Event</c> for a message or signal, <c>Delay</c>
/// for a timer -- so what fires one is what fires it in a deployment: <see cref="Elsa.Workflows.Runtime.IEventPublisher"/>
/// for a message or signal, and resuming the timer's bookmark, which is what the scheduler does when it falls due. A
/// message is published for the instance's correlation id; a signal is a broadcast and is published with none.
/// </remarks>
public class BpmnCatchEventDeliveryTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper);

    public Task InitializeAsync() => _host.InitializeAsync();

    public Task DisposeAsync() => _host.DisposeAsync();

    [Fact(DisplayName = "An event-based gateway race won by the published message withdraws the timer and its bookmark")]
    public async Task EventBasedGateway_MessageWins_WithdrawsTheTimer()
    {
        // Arrange: both catches are armed.
        var (instanceId, payment, deadline) = await StartRaceAsync("order-1");

        // Act: the message arrives before the timer is due.
        await _host.PublishEventAsync("PaymentReceived", correlationId: "order-1");

        // Assert: the message path ran, the timer was withdrawn, and nothing is left for it to fire into.
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Completed, statuses[payment]);
        Assert.Equal(ActivityStatus.Canceled, statuses[deadline]);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId("payment-race", "Ship")]);
        Assert.False(statuses.ContainsKey(WorkActivityId("payment-race", "CancelOrder")));
    }

    [Fact(DisplayName = "An event-based gateway race won by the timer withdraws the message catch, so a late message changes nothing")]
    public async Task EventBasedGateway_TimerWins_WithdrawsTheMessage()
    {
        // Arrange
        var (instanceId, payment, deadline) = await StartRaceAsync("order-2");

        // Act: the timer falls due first.
        await _host.ResumeAsync(instanceId, deadline);

        // Assert
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Completed, statuses[deadline]);
        Assert.Equal(ActivityStatus.Canceled, statuses[payment]);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId("payment-race", "CancelOrder")]);
        Assert.False(statuses.ContainsKey(WorkActivityId("payment-race", "Ship")));

        // And the withdrawn catch really is gone: the message arriving late resumes nothing.
        await _host.PublishEventAsync("PaymentReceived", correlationId: "order-2");

        Assert.False((await _host.ActivityStatusesOfAsync(instanceId)).ContainsKey(WorkActivityId("payment-race", "Ship")));
    }

    [Fact(DisplayName = "A non-interrupting signal boundary fires its path while the receive task keeps waiting, and the task's own message still completes it")]
    public async Task NonInterruptingSignalBoundary_FiresWhileTheTaskKeepsWaiting()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("catch-boundary-events.bpmn");
        var instanceId = await _host.StartAsync(definitionId, correlationId: "order-3");

        AssertArmed(await _host.StateOfAsync(instanceId), "AwaitReview", "Cancelled", "ReminderDue");

        // Act: the reminder signal is broadcast.
        await _host.PublishEventAsync("ReminderDue");

        // Assert: the reminder path ran, and the task and its interrupting boundary are still waiting.
        var reminded = await _host.StateOfAsync(instanceId);
        Assert.True(IsSuspended(reminded));
        Assert.Equal(ActivityStatus.Completed, (await _host.ActivityStatusesOfAsync(instanceId))[ReviewWork("RemindReviewer")]);
        Assert.Contains(reminded.Bookmarks, bookmark => bookmark.ActivityId == ReviewWork("AwaitReview"));
        Assert.Contains(reminded.Bookmarks, bookmark => bookmark.ActivityId == ReviewWork("Cancelled"));

        // Act: the task's own message arrives.
        await _host.PublishEventAsync("ReviewCompleted", correlationId: "order-3");

        // Assert: the task completed down its own path, and the boundary it no longer needs was retired.
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Completed, statuses[ReviewWork("Fulfil")]);
        Assert.Equal(ActivityStatus.Canceled, statuses[ReviewWork("Cancelled")]);
        Assert.False(statuses.ContainsKey(ReviewWork("Refund")));
    }

    [Fact(DisplayName = "An interrupting message boundary tears down the receive task it is attached to and routes its own path")]
    public async Task InterruptingMessageBoundary_TearsTheTaskDown()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("catch-boundary-events.bpmn");
        var instanceId = await _host.StartAsync(definitionId, correlationId: "order-4");

        // Act: the cancellation message arrives while the task is still waiting.
        await _host.PublishEventAsync("OrderCancelled", correlationId: "order-4");

        // Assert: the task and the other boundary were withdrawn, the cancellation path ran, the task's path never did.
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Canceled, statuses[ReviewWork("AwaitReview")]);
        Assert.Equal(ActivityStatus.Canceled, statuses[ReviewWork("ReminderDue")]);
        Assert.Equal(ActivityStatus.Completed, statuses[ReviewWork("Refund")]);
        Assert.False(statuses.ContainsKey(ReviewWork("Fulfil")));
    }

    [Fact(DisplayName = "A non-interrupting signal event subprocess runs its body on each broadcast while the process keeps waiting")]
    public async Task NonInterruptingSignalEventSubprocess_RunsOnEachBroadcast()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("signal-event-subprocesses.bpmn");
        var instanceId = await _host.StartAsync(definitionId, correlationId: "shipment-1");

        AssertArmed(await _host.StateOfAsync(instanceId), "AwaitShipment", "OnStatusRequested-listener", "OnRecall-listener");

        // Act: the status signal is broadcast twice.
        await _host.PublishEventAsync("StatusRequested");
        await _host.PublishEventAsync("StatusRequested");

        // Assert: the body ran once per broadcast, and the process is still waiting where it was.
        var waiting = await _host.StateOfAsync(instanceId);
        Assert.True(IsSuspended(waiting));
        Assert.Equal(2, (await _host.ExecutionCountsOfAsync(instanceId))[WorkActivityId("OnStatusRequested", "ReportStatus")]);
        Assert.Contains(waiting.Bookmarks, bookmark => bookmark.ActivityId == WorkActivityId("track-shipment", "AwaitShipment"));

        // Act: the shipment arrives.
        await _host.PublishEventAsync("ShipmentArrived", correlationId: "shipment-1");

        // Assert: the process finished down its own path, retiring both listeners.
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId("track-shipment", "ConfirmDelivery")]);
        Assert.False(statuses.ContainsKey(WorkActivityId("OnRecall", "HandleRecall")));
    }

    [Fact(DisplayName = "An interrupting signal event subprocess stops the process's waiting work and runs its body instead")]
    public async Task InterruptingSignalEventSubprocess_StopsTheProcess()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("signal-event-subprocesses.bpmn");
        var instanceId = await _host.StartAsync(definitionId, correlationId: "shipment-2");

        // Act: the recall signal is broadcast.
        await _host.PublishEventAsync("RecallIssued");

        // Assert: the waiting work was withdrawn, the recall body ran, and the process never reached its own path.
        var state = await _host.StateOfAsync(instanceId);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);

        AssertFinished(state);
        Assert.Equal(ActivityStatus.Canceled, statuses[WorkActivityId("track-shipment", "AwaitShipment")]);
        Assert.Equal(ActivityStatus.Completed, statuses[WorkActivityId("OnRecall", "HandleRecall")]);
        Assert.False(statuses.ContainsKey(WorkActivityId("track-shipment", "ConfirmDelivery")));
    }

    private async Task<(string InstanceId, string Payment, string Deadline)> StartRaceAsync(string correlationId)
    {
        var definitionId = await _host.ImportAndPublishAsync("event-gateway-race.bpmn");
        var instanceId = await _host.StartAsync(definitionId, correlationId);

        AssertArmed(await _host.StateOfAsync(instanceId), "PaymentReceived", "PaymentDeadline");

        return (instanceId, WorkActivityId("payment-race", "PaymentReceived"), WorkActivityId("payment-race", "PaymentDeadline"));
    }

    private static string ReviewWork(string elementId) => WorkActivityId("review-order", elementId);

    /// <summary>Asserts the instance is suspended on exactly the given catches, read by element id under any scope.</summary>
    private static void AssertArmed(WorkflowState state, params string[] elementIds)
    {
        Assert.True(IsSuspended(state));
        Assert.Equal(
            elementIds.Select(elementId => $"node-{elementId}").Order(StringComparer.Ordinal),
            state.Bookmarks.Select(bookmark => bookmark.ActivityId[(bookmark.ActivityId.IndexOf(':') + 1)..]).Order(StringComparer.Ordinal));
    }

    private static void AssertFinished(WorkflowState state)
    {
        Assert.Empty(state.Incidents);
        Assert.Empty(state.Bookmarks);
        Assert.Equal(WorkflowSubStatus.Finished, state.SubStatus);
    }
}
