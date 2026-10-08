using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.Slack.Activities.Events;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using SlackNet.WebApi;
using Xunit.Abstractions;

namespace Elsa.Slack.Tests.Activities.Events;

public sealed class WatchPublicChannelMessagesTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(null, "hello")]
    [InlineData("1700000000.000001", "")]
    public async Task AdmittedModePreservesIncomingMessageAndSeparateReplyAnchor(string? thread, string text)
    {
        var source = new MessageSource(new("C_TEST", text, "1700000001.000002", "U_HUMAN", thread, "U_SELF"));
        var fixture = CreateFixture(source);
        await fixture.BuildAsync();
        await using var lifetime = (IAsyncDisposable)fixture.Services;
        var activity = CreateWatch();
        var result = await fixture.RunActivityAsync(activity);
        Assert.Empty(result.WorkflowState.Incidents);
        var context = Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity.Id == activity.Id);
        var message = Assert.IsType<Message>(context.GetActivityOutput(() => activity.ReceivedMessage));
        Assert.Equal("C_TEST", message.Channel);
        Assert.Equal(text, message.Text);
        Assert.Equal(thread, message.ThreadTs);
        Assert.Equal("1700000001.000002", context.GetActivityOutput(() => activity.MessageTimestamp));
        Assert.Equal("U_HUMAN", context.GetActivityOutput(() => activity.UserId));
        Assert.Equal(thread ?? "1700000001.000002", context.GetActivityOutput(() => activity.ReplyThreadTimestamp));
        Assert.Equal(1, source.Reads);
    }

    [Theory]
    [InlineData("token", "C_TEST", "U_SELF", 1, 0)]
    [InlineData(" ", "C_TEST", "U_SELF", 1, 0)]
    [InlineData("", "C_OTHER", "U_SELF", 1, 1)]
    [InlineData("", "C_TEST", "U_OTHER", 1, 1)]
    [InlineData("", "C_TEST", "U_SELF", 42, 0)]
    [InlineData("", "C_TEST", "U_SELF", 0, 0)]
    public async Task UnsupportedModeTokensOrOverridesCannotWriteOutputs(string token, string channel, string self, int mode, int expectedReads)
    {
        var source = new MessageSource(new("C_TEST", "hello", "1700000001.000002", "U_HUMAN", null, "U_SELF"));
        var fixture = CreateFixture(source);
        await fixture.BuildAsync();
        await using var lifetime = (IAsyncDisposable)fixture.Services;
        var activity = CreateWatch();
        activity.Token = new(token);
        activity.ChannelId = new(channel);
        activity.BotUserId = new(self);
        activity.Mode = new((WatchPublicChannelMessageMode)mode);
        var result = await fixture.RunActivityAsync(activity);
        Assert.NotEmpty(result.WorkflowState.Incidents);
        var context = Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity.Id == activity.Id);
        Assert.Null(context.GetActivityOutput(() => activity.ReceivedMessage));
        Assert.Null(context.GetActivityOutput(() => activity.MessageTimestamp));
        Assert.Null(context.GetActivityOutput(() => activity.UserId));
        Assert.Null(context.GetActivityOutput(() => activity.ReplyThreadTimestamp));
        Assert.Equal(expectedReads, source.Reads);
        if (mode == 0)
        {
            Assert.Contains(result.WorkflowState.Incidents, x => x.Message.Contains("Event subscription requires WebSocket implementation.", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task DefinitionsWithoutModeKeepLegacyIdentityAndInputs()
    {
        var fixture = CreateFixture(new MessageSource(new("C_TEST", "unused", "1700000001.000002", "U_HUMAN", null, "U_SELF")));
        await fixture.BuildAsync();
        await using var lifetime = (IAsyncDisposable)fixture.Services;
        var serializer = fixture.Services.GetRequiredService<IActivitySerializer>();
        var activity = CreateWatch();
        activity.Token = new("legacy-token");
        var serialized = JsonNode.Parse(serializer.Serialize(activity))!.AsObject();
        // Remove the actual additive input from the serialized definition.
        var modeName = Assert.Single(serialized.Select(x => x.Key).Where(x => string.Equals(x, "mode", StringComparison.OrdinalIgnoreCase)));
        serialized.Remove(modeName);
        var restored = Assert.IsType<WatchPublicChannelMessages>(serializer.Deserialize(serialized.ToJsonString()));
        Assert.IsAssignableFrom<SlackEventActivity>(restored);
        var roundTrip = serializer.Serialize(restored);
        Assert.Contains("legacy-token", roundTrip, StringComparison.Ordinal);
        Assert.Contains("C_TEST", roundTrip, StringComparison.Ordinal);
        Assert.Contains("U_SELF", roundTrip, StringComparison.Ordinal);
        var result = await fixture.RunActivityAsync(restored);
        Assert.Contains(result.WorkflowState.Incidents, x => x.Message.Contains("Event subscription requires WebSocket implementation.", StringComparison.Ordinal));
    }

    private WorkflowTestFixture CreateFixture(ISlackPublicChannelMessageSource source) => new WorkflowTestFixture(output)
        .ConfigureElsa(elsa => elsa.AddActivity<WatchPublicChannelMessages>())
        .ConfigureServices(services => services.AddSingleton(source));

    private static WatchPublicChannelMessages CreateWatch() => new()
    {
        Mode = new(WatchPublicChannelMessageMode.AdmittedPublicMessage), Token = new(""),
        ChannelId = new("C_TEST"), BotUserId = new("U_SELF")
    };

    private sealed class MessageSource(SlackPublicChannelMessage message) : ISlackPublicChannelMessageSource
    {
        public int Reads { get; private set; }
        public ValueTask<SlackPublicChannelMessage> ReadAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            Reads++;
            return ValueTask.FromResult(message);
        }
    }
}
