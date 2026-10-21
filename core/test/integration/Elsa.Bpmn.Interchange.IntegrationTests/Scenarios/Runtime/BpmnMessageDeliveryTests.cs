using Elsa.Workflows;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// BPMN messages delivered end to end through Elsa's own event machinery: an intermediate message catch, which the
/// binder binds to <c>Event</c>, resumed by <see cref="Elsa.Workflows.Runtime.IEventPublisher"/>; and an intermediate
/// message throw, which it binds to <c>PublishEvent</c>, delivering to another instance waiting on that catch.
/// </summary>
public class BpmnMessageDeliveryTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private const string CatchProcessId = "await-payment";

    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper);

    public Task InitializeAsync() => _host.InitializeAsync();

    public Task DisposeAsync() => _host.DisposeAsync();

    [Fact(DisplayName = "An intermediate message catch is resumed by publishing the message for the instance's correlation id, and only that instance")]
    public async Task IntermediateMessageCatch_IsResumedByAPublishedEventForItsCorrelationId()
    {
        // Arrange: two instances wait for the same message, told apart only by their correlation ids.
        var definitionId = await _host.ImportAndPublishAsync("message-catch-process.bpmn");
        var paid = await _host.StartAsync(definitionId, correlationId: "order-1");
        var unpaid = await _host.StartAsync(definitionId, correlationId: "order-2");

        var awaitPayment = WorkActivityId(CatchProcessId, "AwaitPayment");
        var recordPayment = WorkActivityId(CatchProcessId, "RecordPayment");

        foreach (var instanceId in new[] { paid, unpaid })
        {
            var waiting = await _host.StateOfAsync(instanceId);
            Assert.True(IsSuspended(waiting));
            Assert.Equal(awaitPayment, Assert.Single(waiting.Bookmarks).ActivityId);
        }

        // Act: the message arrives for one of them.
        await _host.PublishEventAsync("PaymentReceived", correlationId: "order-1");

        // Assert: that instance carried on past the catch and finished...
        var finished = await _host.StateOfAsync(paid);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);
        Assert.Equal(ActivityStatus.Completed, (await _host.ActivityStatusesOfAsync(paid))[recordPayment]);

        // ...and the other is still waiting on its own catch, untouched.
        var stillWaiting = await _host.StateOfAsync(unpaid);
        Assert.True(IsSuspended(stillWaiting));
        Assert.Equal(awaitPayment, Assert.Single(stillWaiting.Bookmarks).ActivityId);
        Assert.False((await _host.ActivityStatusesOfAsync(unpaid)).ContainsKey(recordPayment));
    }

    [Fact(DisplayName = "An intermediate message throw delivers its message to another instance waiting on the matching catch")]
    public async Task IntermediateMessageThrow_DeliversToAnotherWaitingInstance()
    {
        // Arrange: a receiver waits on the catch.
        var receiverDefinitionId = await _host.ImportAndPublishAsync("message-catch-process.bpmn");
        var senderDefinitionId = await _host.ImportAndPublishAsync("message-throw-process.bpmn");
        var receiver = await _host.StartAsync(receiverDefinitionId, correlationId: "order-3");

        Assert.True(IsSuspended(await _host.StateOfAsync(receiver)));

        // Act: the sender runs its throw event.
        var sender = await _host.StartAsync(senderDefinitionId);

        // Assert: the sender ran its publish and finished without waiting for anyone...
        var sent = await _host.StateOfAsync(sender);
        Assert.Equal(WorkflowSubStatus.Finished, sent.SubStatus);
        Assert.Empty(sent.Incidents);
        Assert.Equal(ActivityStatus.Completed, (await _host.ActivityStatusesOfAsync(sender))[WorkActivityId("announce-payment", "AnnouncePayment")]);

        // ...and the message reached the receiver, which resumed past its catch and finished.
        var received = await _host.WaitForAsync(receiver, IsFinished);
        Assert.Equal(WorkflowSubStatus.Finished, received.SubStatus);
        Assert.Empty(received.Incidents);
        Assert.Equal(ActivityStatus.Completed, (await _host.ActivityStatusesOfAsync(receiver))[WorkActivityId(CatchProcessId, "RecordPayment")]);
    }
}
