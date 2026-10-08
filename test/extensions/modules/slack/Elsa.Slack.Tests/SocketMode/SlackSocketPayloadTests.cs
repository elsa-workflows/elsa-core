using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Slack.SocketMode.Events;

namespace Elsa.Slack.Tests.SocketMode;

public class SlackSocketPayloadTests
{
    [Fact]
    public void HumanProjectionPreservesEmptyTextAndOriginalTimestampPaths()
    {
        var message = """{"type":"message","channel":"C_TEST","user":"U_HUMAN","text":"","ts":"1700000000.000001","thread_ts":"1699999999.000002"}""";
        var payload = SocketModeTestData.Event(message);
        var restored = SlackSocketEventPayload.DeserializeValidatedHuman(JsonSerializer.Serialize(payload));
        Assert.Equal("", restored.GetRequiredString("text"));
        Assert.Equal("1700000000.000001", restored.GetRequiredString("ts"));
        Assert.Equal("1699999999.000002", restored.GetOptionalString("thread_ts"));
        Assert.Null(restored.GetOptionalString("message.ts"));
        Assert.Equal(SlackSocketEventPayload.ProjectionPaths.Length, restored.Projection.Count);
    }

    [Fact]
    public void CanonicalObjectOrderAndEquivalentDecodedStringsMatch()
    {
        var first = """{"type":"message","channel":"C_TEST","user":"U_HUMAN","text":"\u0068ello","ts":"1700000000.000001","unknown":{"b":2,"a":1}}""";
        var second = """{"unknown":{"a":1,"b":2},"ts":"1700000000.000001","text":"hello","user":"U_HUMAN","channel":"C_TEST","type":"message"}""";
        Assert.Equal(SocketModeTestData.Event(first).SourceEventDigest, SocketModeTestData.Event(second).SourceEventDigest);
    }

    [Theory]
    [InlineData("1", "1.0")]
    [InlineData("1", "1e0")]
    [InlineData("0", "-0")]
    [InlineData("[1,2]", "[2,1]")]
    [InlineData("null", "\"\"")]
    [InlineData("{}", "null")]
    public void CanonicalDigestRetainsNumberSpellingArrayOrderAndJsonKinds(string first, string second)
    {
        string Message(string value) => SocketModeTestData.Human[..^1] + ",\"unknown\":" + value + "}";
        Assert.NotEqual(SocketModeTestData.Event(Message(first)).SourceEventDigest, SocketModeTestData.Event(Message(second)).SourceEventDigest);
    }

    [Theory]
    [InlineData("hello", " hello ")]
    [InlineData("\u00e9", "e\u0301")]
    public void TextIsNeverTrimmedOrUnicodeNormalized(string first, string second)
    {
        var a = SocketModeTestData.Change(SocketModeTestData.Human, x => x["text"] = first);
        var b = SocketModeTestData.Change(SocketModeTestData.Human, x => x["text"] = second);
        Assert.Equal(first, SocketModeTestData.Event(a).GetRequiredString("text"));
        Assert.Equal(second, SocketModeTestData.Event(b).GetRequiredString("text"));
        Assert.NotEqual(SocketModeTestData.Event(a).SourceEventDigest, SocketModeTestData.Event(b).SourceEventDigest);
    }

    [Fact]
    public void MissingEmptyNullAndUnknownProviderFieldsCannotCollapse()
    {
        var bot = """{"type":"message","channel":"C_TEST","subtype":"bot_message"}""";
        var absent = SocketModeTestData.Event(bot);
        var empty = SocketModeTestData.Event(SocketModeTestData.Change(bot, x => x["text"] = ""));
        Assert.False(absent.Projection.Single(x => x.Path == "text").Present);
        Assert.Equal("", empty.GetRequiredString("text"));
        Assert.NotEqual(absent.SourceEventDigest, empty.SourceEventDigest);
        var withNull = SocketModeTestData.Event(SocketModeTestData.Change(bot, x => x["unknown"] = null));
        var withEmpty = SocketModeTestData.Event(SocketModeTestData.Change(bot, x => x["unknown"] = ""));
        Assert.Equal(4, new[] { absent.SourceEventDigest, empty.SourceEventDigest, withNull.SourceEventDigest, withEmpty.SourceEventDigest }.Distinct().Count());
    }

    [Fact]
    public void EditOuterAndNestedTimestampPathsRemainDistinct()
    {
        var message = """{"type":"message","channel":"C_TEST","subtype":"message_changed","ts":"1700000001.000001","message":{"type":"message","ts":"1700000000.000002"}}""";
        var first = SocketModeTestData.Event(message);
        var swapped = SocketModeTestData.Event(SocketModeTestData.Change(message, x =>
        {
            x["ts"] = "1700000000.000002";
            x["message"]!["ts"] = "1700000001.000001";
        }));
        Assert.Equal("1700000001.000001", first.GetRequiredString("ts"));
        Assert.Equal("1700000000.000002", first.GetRequiredString("message.ts"));
        Assert.NotEqual(first.SourceEventDigest, swapped.SourceEventDigest);
    }

    [Fact]
    public void EnvelopeRetryAndAuthorizationOrderDoNotRedefineNormalizedEvent()
    {
        var first = SocketModeTestData.Change(SocketModeTestData.Envelope(), root =>
        {
            root["retry_attempt"] = 1;
            root["payload"]!["authorizations"] = JsonNode.Parse("""[{"user_id":"U_ONE"},{"user_id":"U_TWO"}]""");
        });
        var second = SocketModeTestData.Change(first, root =>
        {
            root["envelope_id"] = "replacement-envelope";
            root["retry_attempt"] = 2;
            root["retry_reason"] = "timeout";
            root["payload"]!["authorizations"] = JsonNode.Parse("""[{"user_id":"U_TWO"},{"user_id":"U_ONE"}]""");
        });
        var a = SocketModeTestData.Parse(first);
        var b = SocketModeTestData.Parse(second);
        Assert.NotEqual(a.EnvelopeId, b.EnvelopeId);
        Assert.Equal(JsonSerializer.Serialize(a.Event), JsonSerializer.Serialize(b.Event));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("kind")]
    [InlineData("extra-root")]
    [InlineData("duplicate-slot")]
    [InlineData("missing-slot")]
    [InlineData("absent-has-value")]
    [InlineData("absent-has-kind")]
    [InlineData("orphan-message-ts")]
    [InlineData("orphan-edited-ts")]
    [InlineData("invalid-optional-event-ts")]
    [InlineData("invalid-optional-previous-message")]
    [InlineData("required-text-wrong-kind")]
    [InlineData("required-ts-invalid")]
    [InlineData("unknown-projection-path")]
    [InlineData("kind-value-mismatch")]
    [InlineData("bot-marker")]
    [InlineData("self-user")]
    [InlineData("both-installation-ids-null")]
    [InlineData("malformed-team")]
    [InlineData("malformed-enterprise")]
    [InlineData("fractional-time")]
    public void NormalizedHumanRejectsSpoofedAndIncoherentShapes(string mutation)
    {
        var payload = SocketModeTestData.NormalizedHuman();
        switch (mutation)
        {
            case "version": payload["Version"] = 2; break;
            case "kind": payload["Kind"] = "Bot"; break;
            case "extra-root": payload["Invocation"] = "not-authority"; break;
            case "duplicate-slot": payload["Projection"]!.AsArray()[1] = payload["Projection"]![0]!.DeepClone(); break;
            case "missing-slot": payload["Projection"]!.AsArray().RemoveAt(0); break;
            case "absent-has-value": SocketModeTestData.Slot(payload, "thread_ts")["Value"] = "1700000000.000001"; break;
            case "absent-has-kind": SocketModeTestData.Slot(payload, "thread_ts")["Kind"] = (int)JsonValueKind.String; break;
            case "orphan-message-ts": SetSlot(payload, "message.ts", JsonValueKind.String, "1700000000.000001"); break;
            case "orphan-edited-ts": SetSlot(payload, "edited.ts", JsonValueKind.String, "1700000000.000001"); break;
            case "invalid-optional-event-ts": SetSlot(payload, "event_ts", JsonValueKind.Null, "null"); break;
            case "invalid-optional-previous-message": SetSlot(payload, "previous_message", JsonValueKind.Null, "null"); break;
            case "required-text-wrong-kind": SetSlot(payload, "text", JsonValueKind.Number, "12"); break;
            case "required-ts-invalid": SetSlot(payload, "ts", JsonValueKind.String, "not-a-timestamp"); break;
            case "unknown-projection-path": SocketModeTestData.Slot(payload, "text")["Path"] = "executable"; break;
            case "kind-value-mismatch": SetSlot(payload, "hidden", JsonValueKind.False, "true"); break;
            case "bot-marker": SetSlot(payload, "bot_id", JsonValueKind.String, "B_TEST"); break;
            case "self-user": SetSlot(payload, "user", JsonValueKind.String, "U_SELF"); break;
            case "both-installation-ids-null": payload["TeamId"] = null; payload["EnterpriseId"] = null; break;
            case "malformed-team": payload["TeamId"] = ""; break;
            case "malformed-enterprise": payload["EnterpriseId"] = "bad\nidentity"; break;
            case "fractional-time": payload["OccurredAt"] = DateTimeOffset.FromUnixTimeSeconds(1700000100).AddTicks(1).ToString("O"); break;
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
        var error = Record.Exception(() => SlackSocketEventPayload.DeserializeValidatedHuman(payload.ToJsonString()));
        Assert.True(error is InvalidDataException or JsonException or ArgumentException,
            "The normalized human shape must be rejected by a data-contract error.");
    }

    private static void SetSlot(JsonObject payload, string path, JsonValueKind kind, string value)
    {
        var slot = SocketModeTestData.Slot(payload, path);
        slot["Present"] = true;
        slot["Kind"] = (int)kind;
        slot["Value"] = value;
    }
}
