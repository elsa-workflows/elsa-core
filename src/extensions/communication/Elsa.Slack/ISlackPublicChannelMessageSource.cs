using Elsa.Workflows;

namespace Elsa.Slack;

/// <summary>
/// Reads an immutable public-channel message for the current activity. The optional ingress adapter
/// supplies this data-only service; it must not expose credentials or an execution capability.
/// </summary>
public interface ISlackPublicChannelMessageSource
{
    ValueTask<SlackPublicChannelMessage> ReadAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>Original message scalars and the trusted self identity; no token or callback is carried.</summary>
public sealed record SlackPublicChannelMessage(string ChannelId, string Text, string MessageTimestamp,
    string UserId, string? ThreadTimestamp, string SelfUserId)
{
    public string ReplyThreadTimestamp => ThreadTimestamp ?? MessageTimestamp;
    public override string ToString() => "SlackPublicChannelMessage { Redacted = true }";
}
