using Elsa.Mqtt.Activities;
using Elsa.Mqtt.Handlers;
using Elsa.Mqtt.Models;
using Elsa.Mqtt.Notifications;
using Elsa.Mqtt.Services;
using Elsa.Mqtt.Stimuli;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using NSubstitute;

namespace Elsa.Mqtt.UnitTests.Handlers;

public class TriggerMqttWorkflowsTests
{
    private readonly ITriggerInvoker _triggerInvoker = Substitute.For<ITriggerInvoker>();
    private readonly IBookmarkQueue _bookmarkQueue = Substitute.For<IBookmarkQueue>();
    private readonly TriggerMqttWorkflows _handler;

    // A real MqttSubscriber backed by a no-op IMqttClient mock so we can
    // pre-populate TriggerBindings / BookmarkBindings without a live broker.
    private readonly MqttSubscriber _subscriber;

    public TriggerMqttWorkflowsTests()
    {
        _handler = new TriggerMqttWorkflows(_triggerInvoker, _bookmarkQueue);

        var mqttClient = Substitute.For<IMqttClient>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logger = Substitute.For<ILogger<MqttSubscriber>>();

        _subscriber = new MqttSubscriber(
            connectionName: "Default",
            mqttClient: mqttClient,
            scopeFactory: scopeFactory,
            logger: logger,
            maxReconnectAttempts: 10,
            reconnectBaseDelay: TimeSpan.FromMilliseconds(100));
    }

    // ---------- Trigger routing ----------

    [Fact]
    public async Task HandleAsync_InvokesTrigger_WhenTopicMatchesExactly()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        var notification = MakeNotification("sensors/temp");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.Received(1).InvokeAsync(Arg.Any<InvokeTriggerRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvokesTrigger_WhenTopicMatchesSingleLevelWildcard()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/+"]));
        var notification = MakeNotification("sensors/humidity");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.Received(1).InvokeAsync(Arg.Any<InvokeTriggerRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvokesTrigger_WhenTopicMatchesMultiLevelWildcard()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["home/#"]));
        var notification = MakeNotification("home/living-room/lamp/status");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.Received(1).InvokeAsync(Arg.Any<InvokeTriggerRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DoesNotInvokeTrigger_WhenTopicDoesNotMatch()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        var notification = MakeNotification("sensors/humidity");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.DidNotReceive().InvokeAsync(Arg.Any<InvokeTriggerRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvokesOnlyMatchingTriggers_WhenMultipleTriggersAreRegistered()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t2", ["sensors/humidity"]));
        var notification = MakeNotification("sensors/temp");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.Received(1).InvokeAsync(Arg.Any<InvokeTriggerRequest>(), Arg.Any<CancellationToken>());
    }

    // ---------- Bookmark routing ----------

    [Fact]
    public async Task HandleAsync_EnqueuesBookmark_WhenTopicMatchesExactly()
    {
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["commands/start"]));
        var notification = MakeNotification("commands/start");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _bookmarkQueue.Received(1).EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DoesNotEnqueueBookmark_WhenTopicDoesNotMatch()
    {
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["commands/start"]));
        var notification = MakeNotification("commands/stop");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EnqueuesBookmark_WithCorrectWorkflowInstanceId()
    {
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["telemetry/#"], workflowInstanceId: "wf-42"));
        var notification = MakeNotification("telemetry/device1/value");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _bookmarkQueue.Received(1).EnqueueAsync(
            Arg.Is<NewBookmarkQueueItem>(item => item.WorkflowInstanceId == "wf-42"),
            Arg.Any<CancellationToken>());
    }

    // ---------- Input / Properties isolation ----------

    [Fact]
    public async Task HandleAsync_TriggerInput_ContainsTransportMessage()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        var notification = MakeNotification("sensors/temp", payload: "42.5");

        await _handler.HandleAsync(notification, CancellationToken.None);

        await _triggerInvoker.Received(1).InvokeAsync(
            Arg.Is<InvokeTriggerRequest>(r =>
                r.Input != null &&
                r.Input.ContainsKey(MqttMessageReceived.InputKey)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TriggerInputAndProperties_AreIndependentDictionaries()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        var notification = MakeNotification("sensors/temp");

        InvokeTriggerRequest? captured = null;
        await _triggerInvoker.InvokeAsync(
            Arg.Do<InvokeTriggerRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await _handler.HandleAsync(notification, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.NotSame(captured!.Input, captured.Properties);
    }

    // ---------- Helpers ----------

    private static MqttTriggerBinding MakeTriggerBinding(string triggerId, ICollection<string> topics) =>
        new(
            Workflow: null!,
            TriggerId: triggerId,
            TriggerActivityId: triggerId + "_act",
            Stimulus: new MqttMessageReceivedStimulus { ConnectionName = "Default", Topics = topics });

    private static MqttBookmarkBinding MakeBookmarkBinding(
        string bookmarkId,
        ICollection<string> topics,
        string workflowInstanceId = "wf1") =>
        new(
            WorkflowInstanceId: workflowInstanceId,
            CorrelationId: null,
            BookmarkId: bookmarkId,
            Stimulus: new MqttMessageReceivedStimulus { ConnectionName = "Default", Topics = topics });

    private MqttMessageReceivedNotification MakeNotification(string topic, string payload = "") =>
        new(_subscriber, new MqttMessage(topic, payload));
}
