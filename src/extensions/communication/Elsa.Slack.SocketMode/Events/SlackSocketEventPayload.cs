using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode.Events;

internal enum SlackSocketEventKind { Human, Bot, Self, Edit, Delete, UnsupportedMessageSubtype }
internal sealed record SlackSocketProjectionValue(string Path, bool Present, JsonValueKind Kind, string? Value);

/// <summary>Only this versioned scalar representation enters Admission; no provider CLR object is activated.</summary>
internal sealed record SlackSocketEventPayload(int Version, string BindingFingerprint, string SourceEventDigest, string Kind,
    string AppId, string? TeamId, string? EnterpriseId, string ProviderEventId, DateTimeOffset OccurredAt,
    string ChannelId, string SelfUserId, IReadOnlyList<SlackSocketProjectionValue> Projection)
{
    internal static readonly string[] ProjectionPaths =
    [
        "user", "text", "ts", "thread_ts", "subtype", "bot_id", "bot_profile", "hidden", "event_ts", "deleted_ts",
        "edited", "edited.ts", "edited.user", "message", "message.type", "message.user", "message.text",
        "message.ts", "message.thread_ts", "previous_message", "previous_message.user", "previous_message.text",
        "previous_message.ts", "previous_message.thread_ts"
    ];

    internal string GetRequiredString(string path)
    {
        var slot = Projection.Single(x => x.Path == path);
        return slot is { Present: true, Kind: JsonValueKind.String, Value: not null }
            ? slot.Value : throw new InvalidDataException("Required Socket projection is absent.");
    }

    internal string? GetOptionalString(string path)
    {
        var slot = Projection.Single(x => x.Path == path);
        return !slot.Present ? null : GetRequiredString(path);
    }

    internal static SlackSocketEventPayload DeserializeValidatedHuman(string payload)
    {
        using var document = SlackSocketJson.Parse(Encoding.UTF8.GetBytes(payload), AdmissionLimits.PayloadBytes, 32);
        var result = document.RootElement.Deserialize<SlackSocketEventPayload>(new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Socket event payload is absent.");
        if (result.Version != 1 || result.Kind != nameof(SlackSocketEventKind.Human) ||
            !IsDigest(result.BindingFingerprint) || !IsDigest(result.SourceEventDigest) ||
            result.Projection is null || result.Projection.Count != ProjectionPaths.Length ||
            result.Projection.Any(x => x is null) ||
            !result.Projection.Select(x => x.Path).OrderBy(x => x, StringComparer.Ordinal)
                .SequenceEqual(ProjectionPaths.OrderBy(x => x, StringComparer.Ordinal)))
        {
            throw new InvalidDataException("Socket event payload contract is invalid.");
        }
        foreach (var slot in result.Projection)
        {
            if (!slot.Present && (slot.Kind != JsonValueKind.Undefined || slot.Value is not null))
            {
                throw new InvalidDataException("Absent Socket projection contains a value.");
            }
        }
        foreach (var value in new[] { result.AppId, result.ProviderEventId, result.ChannelId, result.SelfUserId })
        {
            SlackSocketModeConfiguration.ValidateIdentifier(value);
        }
        if (result.OccurredAt <= DateTimeOffset.UnixEpoch ||
            result.GetRequiredString("user") == result.SelfUserId ||
            result.Projection.Any(x => x.Present && x.Path is "subtype" or "bot_id" or "bot_profile" or "edited" or "deleted_ts" or "message") ||
            result.Projection.Any(x => x.Path == "hidden" && x.Present && (x.Kind != JsonValueKind.False || x.Value != "false")))
        {
            throw new InvalidDataException("Socket payload is not an original human message.");
        }
        SlackSocketModeConfiguration.ValidateIdentifier(result.GetRequiredString("user"));
        _ = result.GetRequiredString("text");
        SlackSocketWireParser.ValidateTimestamp(result.GetRequiredString("ts"));
        var thread = result.GetOptionalString("thread_ts");
        if (thread is not null)
        {
            SlackSocketWireParser.ValidateTimestamp(thread);
        }
        return result;
    }

    private static bool IsDigest(string value) => value is { Length: 64 } && value.All(x => x is >= '0' and <= '9' or >= 'a' and <= 'f');
}
