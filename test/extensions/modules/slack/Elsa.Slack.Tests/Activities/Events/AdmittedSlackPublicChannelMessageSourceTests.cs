using System.Text.Json;
using Elsa.Extensions;
using Elsa.Slack.Activities.Events;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.Tests.SocketMode;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using SlackNet.WebApi;
using Xunit.Abstractions;

namespace Elsa.Slack.Tests.Activities.Events;

// Projection/route tests use a data-only stub. The separate PostgreSQL tests prove the actual
// reader's private consumed-owner gate; this fixture cannot establish that authority itself.
public sealed class AdmittedSlackPublicChannelMessageSourceTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("valid")]
    [InlineData("input-event")]
    [InlineData("input-id")]
    [InlineData("input-channel")]
    [InlineData("epoch")]
    [InlineData("subscription")]
    [InlineData("configuration")]
    [InlineData("provider-id")]
    [InlineData("occurrence")]
    [InlineData("binding-fingerprint")]
    [InlineData("instance")]
    public async Task OnlyCoherentDurableEventAndListenerBindingsProduceOutputs(string mutation)
    {
        var configuration = SocketModeTestData.Configuration();
        var normalized = SocketModeTestData.Event();
        var payload = JsonSerializer.Serialize(normalized);
        var data = new AdmissionExecutionData("admission", "instance", configuration.Subscriptions[0].Configuration,
            1, normalized.ProviderEventId, normalized.OccurredAt, payload, AdmissionHash.Compute(payload), "event-fingerprint");
        var input = new Dictionary<string, object>
        {
            ["Event"] = payload, ["ProviderEventId"] = normalized.ProviderEventId, ["ChannelId"] = configuration.ChannelId
        };
        switch (mutation)
        {
            case "input-event": input["Event"] = "different"; break;
            case "input-id": input["ProviderEventId"] = "different"; break;
            case "input-channel": input["ChannelId"] = "different"; break;
            case "epoch": data = data with { ActivationEpoch = 2 }; break;
            case "subscription": data = data with { Configuration = data.Configuration with { Id = "other-subscription" } }; break;
            case "configuration": data = data with { Configuration = data.Configuration with { DefinitionVersion = 2 } }; break;
            case "provider-id":
                data = data with { ProviderEventId = "different" };
                input["ProviderEventId"] = data.ProviderEventId;
                break;
            case "occurrence": data = data with { OccurredAt = data.OccurredAt.AddSeconds(1) }; break;
            case "binding-fingerprint":
                payload = JsonSerializer.Serialize(normalized with { BindingFingerprint = new string('b', 64) });
                data = data with { Payload = payload, PayloadFingerprint = AdmissionHash.Compute(payload) };
                input["Event"] = payload;
                break;
        }
        var reader = new DataReader(data, mutation == "instance");
        var fixture = new WorkflowTestFixture(output)
            .ConfigureElsa(elsa => elsa.AddActivity<WatchPublicChannelMessages>())
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
                services.AddSingleton<IAdmissionExecutionDataReader>(reader);
                services.AddScoped<ISlackPublicChannelMessageSource, AdmittedSlackPublicChannelMessageSource>();
            });
        await fixture.BuildAsync();
        await using var lifetime = (IAsyncDisposable)fixture.Services;
        var activity = new WatchPublicChannelMessages
        {
            Mode = new(WatchPublicChannelMessageMode.AdmittedPublicMessage), Token = new(""),
            ChannelId = new(configuration.ChannelId), BotUserId = new(configuration.SelfUserId)
        };
        var result = await fixture.RunActivityAsync(activity, new RunWorkflowOptions { Input = input });
        var context = Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity.Id == activity.Id);
        Assert.Equal(1, reader.Reads);
        if (mutation == "valid")
        {
            Assert.Empty(result.WorkflowState.Incidents);
            var message = Assert.IsType<Message>(context.GetActivityOutput(() => activity.ReceivedMessage));
            Assert.Equal("hello", message.Text);
            Assert.Equal(configuration.ChannelId, message.Channel);
            Assert.Equal("U_HUMAN", context.GetActivityOutput(() => activity.UserId));
            Assert.Equal("1700000000.000001", context.GetActivityOutput(() => activity.MessageTimestamp));
            Assert.Equal("1700000000.000001", context.GetActivityOutput(() => activity.ReplyThreadTimestamp));
        }
        else
        {
            Assert.NotEmpty(result.WorkflowState.Incidents);
            Assert.Null(context.GetActivityOutput(() => activity.ReceivedMessage));
            Assert.Null(context.GetActivityOutput(() => activity.UserId));
            Assert.Null(context.GetActivityOutput(() => activity.MessageTimestamp));
            Assert.Null(context.GetActivityOutput(() => activity.ReplyThreadTimestamp));
        }
    }

    private sealed class DataReader(AdmissionExecutionData data, bool foreignInstance) : IAdmissionExecutionDataReader
    {
        public int Reads { get; private set; }
        public ValueTask<AdmissionExecutionData> ReadConsumedEventAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(data with { WorkflowInstanceId = foreignInstance ? "foreign-instance" : context.Id });
        }
    }
}
