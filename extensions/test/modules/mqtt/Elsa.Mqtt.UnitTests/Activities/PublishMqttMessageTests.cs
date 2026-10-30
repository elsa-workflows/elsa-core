using Elsa.Extensions;
using Elsa.Mqtt.Activities;
using Elsa.Mqtt.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Protocol;
using NSubstitute;

namespace Elsa.Mqtt.UnitTests.Activities;

public class PublishMqttMessageTests
{
    // ---------- Precondition failures ----------

    [Fact]
    public async Task ExecuteAsync_CompletesWithFailureOutcome_WhenTopicIsEmpty()
    {
        var mqttClient = Substitute.For<IMqttClient>();
        mqttClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null, []));

        var activity = new PublishMqttMessage
        {
            Topic = new(""),
            Message = new("hello"),
        };
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped<IMqttClientFactory>(_ => new FakeMqttClientFactory(mqttClient));

        var context = await fixture.ExecuteAsync();

        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Equal(false, context.GetResult(activity));
        Assert.True(context.HasOutcome("Failure"));
        Assert.False(context.HasOutcome("Success"));
    }

    [Fact]
    public async Task ExecuteAsync_CompletesWithFailureOutcome_WhenTopicIsWhitespace()
    {
        var mqttClient = Substitute.For<IMqttClient>();
        mqttClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null, []));

        var activity = new PublishMqttMessage
        {
            Topic = new("   "),
            Message = new("hello"),
        };
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped<IMqttClientFactory>(_ => new FakeMqttClientFactory(mqttClient));

        var context = await fixture.ExecuteAsync();

        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Equal(false, context.GetResult(activity));
        Assert.True(context.HasOutcome("Failure"));
        Assert.False(context.HasOutcome("Success"));
    }

    // ---------- Broker responses ----------

    [Fact]
    public async Task ExecuteAsync_CompletesWithSuccessOutcome_WhenBrokerRespondsWithSuccess()
    {
        var mqttClient = Substitute.For<IMqttClient>();
        mqttClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null, []));

        var activity = new PublishMqttMessage
        {
            Topic = new("sensors/temp"),
            Message = new("42.5"),
            QualityOfServiceLevel = new(MqttQualityOfServiceLevel.AtMostOnce),
            Retain = new(false),
        };
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped<IMqttClientFactory>(_ => new FakeMqttClientFactory(mqttClient));

        var context = await fixture.ExecuteAsync();

        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Equal(true, context.GetResult(activity));
        Assert.True(context.HasOutcome("Success"));
        Assert.False(context.HasOutcome("Failure"));
    }

    [Fact]
    public async Task ExecuteAsync_CompletesWithFailureOutcome_WhenBrokerRespondsWithError()
    {
        var mqttClient = Substitute.For<IMqttClient>();
        mqttClient.PublishAsync(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(null, MqttClientPublishReasonCode.UnspecifiedError, "error", []));

        var activity = new PublishMqttMessage
        {
            Topic = new("sensors/temp"),
            Message = new("42.5"),
        };
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped<IMqttClientFactory>(_ => new FakeMqttClientFactory(mqttClient));

        var context = await fixture.ExecuteAsync();

        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Equal(false, context.GetResult(activity));
        Assert.True(context.HasOutcome("Failure"));
        Assert.False(context.HasOutcome("Success"));
    }

    [Fact]
    public async Task ExecuteAsync_PublishesToCorrectTopic()
    {
        MqttApplicationMessage? captured = null;
        var mqttClient = Substitute.For<IMqttClient>();
        mqttClient.PublishAsync(Arg.Do<MqttApplicationMessage>(m => captured = m), Arg.Any<CancellationToken>())
            .Returns(new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null, []));

        var activity = new PublishMqttMessage
        {
            Topic = new("home/lights"),
            Message = new("ON"),
        };
        var fixture = new ActivityTestFixture(activity);
        fixture.Services.AddScoped<IMqttClientFactory>(_ => new FakeMqttClientFactory(mqttClient));

        await fixture.ExecuteAsync();

        Assert.NotNull(captured);
        Assert.Equal("home/lights", captured!.Topic);
    }
}
