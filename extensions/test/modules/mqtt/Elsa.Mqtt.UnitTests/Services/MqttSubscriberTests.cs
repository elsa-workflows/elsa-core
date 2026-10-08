using Elsa.Mqtt.Models;
using Elsa.Mqtt.Services;
using Elsa.Mqtt.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using NSubstitute;

namespace Elsa.Mqtt.UnitTests.Services;

public class MqttSubscriberTests
{
    private readonly IMqttClient _mqttClient = Substitute.For<IMqttClient>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<MqttSubscriber> _logger = Substitute.For<ILogger<MqttSubscriber>>();
    private readonly MqttSubscriber _subscriber;

    public MqttSubscriberTests()
    {
        _subscriber = new MqttSubscriber(
            connectionName: "Default",
            mqttClient: _mqttClient,
            scopeFactory: _scopeFactory,
            logger: _logger,
            maxReconnectAttempts: 10,
            reconnectBaseDelay: TimeSpan.FromMilliseconds(100));
    }

    // ---------- BindTriggerAsync ----------

    [Fact]
    public async Task BindTriggerAsync_SubscribesToTopicOnBroker()
    {
        var binding = MakeTriggerBinding("t1", ["sensors/temp"]);

        await _subscriber.BindTriggerAsync(binding);

        await _mqttClient.Received(1).SubscribeAsync(
            Arg.Is<MqttClientSubscribeOptions>(o => o.TopicFilters.Any(f => f.Topic == "sensors/temp")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindTriggerAsync_DoesNotResubscribeAlreadySubscribedTopic()
    {
        // Same topic in two different trigger bindings.
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t2", ["sensors/temp"]));

        // SubscribeAsync must only be called once for the shared topic.
        await _mqttClient.Received(1).SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindTriggerAsync_SubscribesEachNewTopic()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t2", ["sensors/humidity"]));

        await _mqttClient.Received(2).SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindTriggerAsync_StoresTriggerBinding()
    {
        var binding = MakeTriggerBinding("t1", ["sensors/temp"]);

        await _subscriber.BindTriggerAsync(binding);

        Assert.True(_subscriber.TriggerBindings.ContainsKey("t1"));
    }

    // ---------- UnbindTriggersAsync ----------

    [Fact]
    public async Task UnbindTriggersAsync_UnsubscribesTopicNoLongerNeeded()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));

        await _subscriber.UnbindTriggersAsync(["t1"]);

        await _mqttClient.Received(1).UnsubscribeAsync(
            Arg.Is<MqttClientUnsubscribeOptions>(o => o.TopicFilters.Contains("sensors/temp")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnbindTriggersAsync_KeepsTopicSubscribedWhenOtherBindingsStillUseIt()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t2", ["sensors/temp"]));

        await _subscriber.UnbindTriggersAsync(["t1"]);

        await _mqttClient.DidNotReceive().UnsubscribeAsync(Arg.Any<MqttClientUnsubscribeOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnbindTriggersAsync_RemovesTriggerBinding()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));

        await _subscriber.UnbindTriggersAsync(["t1"]);

        Assert.False(_subscriber.TriggerBindings.ContainsKey("t1"));
    }

    // ---------- BindBookmarkAsync ----------

    [Fact]
    public async Task BindBookmarkAsync_SubscribesToTopicWhenNoTriggerExists()
    {
        var binding = MakeBookmarkBinding("bm1", ["commands/+"]);

        await _subscriber.BindBookmarkAsync(binding);

        await _mqttClient.Received(1).SubscribeAsync(
            Arg.Is<MqttClientSubscribeOptions>(o => o.TopicFilters.Any(f => f.Topic == "commands/+")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BindBookmarkAsync_DoesNotResubscribeTopicAlreadyCoveredByTrigger()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["sensors/temp"]));

        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["sensors/temp"]));

        // Only one SubscribeAsync call (from the trigger), not a second for the bookmark.
        await _mqttClient.Received(1).SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>());
    }

    // ---------- UnbindBookmarksAsync ----------

    [Fact]
    public async Task UnbindBookmarksAsync_UnsubscribesTopicNoLongerNeeded()
    {
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["events/alert"]));

        await _subscriber.UnbindBookmarksAsync(["bm1"]);

        await _mqttClient.Received(1).UnsubscribeAsync(
            Arg.Is<MqttClientUnsubscribeOptions>(o => o.TopicFilters.Contains("events/alert")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnbindBookmarksAsync_RemovesBookmarkBinding()
    {
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["events/alert"]));

        await _subscriber.UnbindBookmarksAsync(["bm1"]);

        Assert.False(_subscriber.BookmarkBindings.ContainsKey("bm1"));
    }

    [Fact]
    public async Task UnbindBookmarksAsync_KeepsTopicSubscribedWhenTriggerStillUsesIt()
    {
        await _subscriber.BindTriggerAsync(MakeTriggerBinding("t1", ["shared/topic"]));
        await _subscriber.BindBookmarkAsync(MakeBookmarkBinding("bm1", ["shared/topic"]));

        await _subscriber.UnbindBookmarksAsync(["bm1"]);

        await _mqttClient.DidNotReceive().UnsubscribeAsync(Arg.Any<MqttClientUnsubscribeOptions>(), Arg.Any<CancellationToken>());
    }

    // ---------- Helpers ----------

    private static MqttTriggerBinding MakeTriggerBinding(string triggerId, ICollection<string> topics) =>
        new(
            Workflow: null!,
            TriggerId: triggerId,
            TriggerActivityId: triggerId + "_act",
            Stimulus: new MqttMessageReceivedStimulus { ConnectionName = "Default", Topics = topics });

    private static MqttBookmarkBinding MakeBookmarkBinding(string bookmarkId, ICollection<string> topics) =>
        new(
            WorkflowInstanceId: "wf1",
            CorrelationId: null,
            BookmarkId: bookmarkId,
            Stimulus: new MqttMessageReceivedStimulus { ConnectionName = "Default", Topics = topics });
}
