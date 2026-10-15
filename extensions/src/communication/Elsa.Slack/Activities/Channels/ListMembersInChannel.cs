using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Lists members of a channel.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Lists all members in a channel.",
    DisplayName = "List Channel Members")]
[UsedImplicitly]
public class ListMembersInChannel : SlackActivity
{
    /// <summary>
    /// The ID of the channel to list members from.
    /// </summary>
    [Input(Name = "Channel Id", Description = "The ID of the channel to list members from.")]
    public Input<string> ChannelId { get; set; } = null!;

    /// <summary>
    /// Number of members to return per page.
    /// </summary>
    [Input(Description = "Number of members to return per page.")]
    public Input<int?> Limit { get; set; } = null!;

    /// <summary>
    /// Paginate through collections of data by setting the cursor parameter to a next_cursor attribute returned by a previous request's response_metadata.
    /// </summary>
    [Input(Description = "Paginate through collections of data by setting the cursor parameter to a next_cursor attribute returned by a previous request's response_metadata.")]
    public Input<string?> Cursor { get; set; } = null!;

    /// <summary>
    /// The list of member IDs in the channel.
    /// </summary>
    [Output(Description = "The list of member IDs in the channel.")]
    public Output<IReadOnlyList<string>> MemberIds { get; set; } = null!;

    /// <summary>
    /// The cursor for the next page of results.
    /// </summary>
    [Output(Name = "Next Cursor", Description = "The cursor for the next page of results.")]
    public Output<string?> NextCursor { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string channelId = context.Get(ChannelId)!;
        int limit = context.Get(Limit) ?? 100;
        string? cursor = context.Get(Cursor);

        ISlackApiClient client = GetClient(context);
        ConversationMembersResponse response = await client.Conversations.Members(
            channelId,
            limit,
            cursor,
            context.CancellationToken);

        context.Set(MemberIds, response.Members);
        context.Set(NextCursor, response.ResponseMetadata?.NextCursor);
    }
}