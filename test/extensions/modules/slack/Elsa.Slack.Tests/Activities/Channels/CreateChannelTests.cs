using System.Reflection;
using Elsa.Extensions;
using Elsa.Slack.Activities.Channels;
using Elsa.Slack.Services;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SlackNet;
using SlackNet.WebApi;
using Xunit.Abstractions;

namespace Elsa.Slack.Tests.Activities.Channels;

/// <summary>
/// Contains tests for the <see cref="CreateChannel"/> activity.
/// </summary>
public class CreateChannelTests(ITestOutputHelper testOutputHelper)
{
    /// <summary>
    /// Tests that the activity forwards its inputs to Slack and stores the returned channel.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync()
    {
        const string token = "offline-test-token";
        const string channelName = "release-notes";
        const string teamId = "T_TEST";
        const string channelId = "C_TEST";

        var conversations = Substitute.For<IConversationsApi>();
        var client = Substitute.For<ISlackApiClient>();
        client.Conversations.Returns(conversations);
        var expectedChannel = new Conversation { Id = channelId, Name = channelName };
        conversations.Create(channelName, true, teamId, Arg.Any<CancellationToken>()).Returns(expectedChannel);

        var slackClientFactory = new SlackClientFactory();
        SeedClient(slackClientFactory, token, client);

        var fixture = new WorkflowTestFixture(testOutputHelper);
        fixture.ConfigureElsa(elsa => elsa.AddActivity<CreateChannel>());
        fixture.ConfigureServices(services => services.AddSingleton(slackClientFactory));
        await fixture.BuildAsync();

        var activity = new CreateChannel
        {
            Token = new Input<string>(token),
            ChannelName = new Input<string>(channelName),
            IsPrivate = new Input<bool>(true),
            TeamId = new Input<string>(teamId)
        };
        var result = await fixture.RunActivityAsync(activity);
        var activityContext = Assert.Single(result.Journal.ActivityExecutionContexts, x => x.Activity.Id == activity.Id);
        var channel = activityContext.GetActivityOutput(() => activity.Channel) as Conversation;

        await conversations.Received(1).Create(channelName, true, teamId, Arg.Any<CancellationToken>());
        Assert.NotNull(channel);
        Assert.Equal(channelId, channel.Id);
        Assert.Equal(channelName, channel.Name);
    }

    private static void SeedClient(SlackClientFactory slackClientFactory, string token, ISlackApiClient client)
    {
        var cacheField = typeof(SlackClientFactory).GetField("_slackClients", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(cacheField);
        Assert.Equal(typeof(Dictionary<string, ISlackApiClient>), cacheField.FieldType);

        var cache = Assert.IsType<Dictionary<string, ISlackApiClient>>(cacheField.GetValue(slackClientFactory));
        Assert.True(cache.TryAdd(token, client));
        Assert.Same(client, slackClientFactory.GetClient(token));
    }
}
