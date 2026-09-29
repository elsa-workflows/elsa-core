using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Channels;

/// <summary>
/// Lists all channels in a workspace.
/// </summary>
[Activity(
    "Elsa.Slack.Channels",
    "Slack Channels",
    "Lists all channels in the workspace.",
    DisplayName = "List Channels")]
[UsedImplicitly]
public class ListChannels : SlackActivity
{
    /// <summary>
    /// Set to true to exclude archived channels from the list.
    /// </summary>
    [Input(Name = "Exclude Archived", Description = "Set to true to exclude archived channels from the list.")]
    public Input<bool> ExcludeArchived { get; set; } = null!;

    /// <summary>
    /// Number of channels to return per page.
    /// </summary>
    [Input(Description = "Number of channels to return per page.")]
    public Input<int?>? Limit { get; set; }

    /// <summary>
    /// The cursor for the next page of results.
    /// </summary>
    [Input(Description = "Paginate through collections of data by setting the cursor parameter to a next_cursor attribute returned by a previous request's response_metadata.")]
    public Input<string>? Cursor { get; set; }

    /// <summary>
    /// The list of channels.
    /// </summary>
    [Output(Description = "The list of channels.")]
    public Output<IReadOnlyList<Conversation>> Channels { get; set; } = null!;

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
        bool excludeArchived = context.Get(ExcludeArchived);
        int limit = context.Get(Limit) ?? 100;
        string? cursor = context.Get(Cursor);

        ISlackApiClient client = GetClient(context);
        ConversationListResponse response = await client.Conversations.List(excludeArchived, limit, cursor: cursor);

        context.Set(Channels, response.Channels);
        context.Set(NextCursor, response.ResponseMetadata?.NextCursor);
    }
}