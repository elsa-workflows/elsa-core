using System.Text;
using System.Text.Json;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Events;

namespace Elsa.Slack.Tests.SocketMode;

public class SlackSocketWireParserTests
{
    public static IEnumerable<object[]> SupportedShapes()
    {
        yield return ["human", SocketModeTestData.Human, "Human"];
        yield return ["human-thread-empty-text", """{"type":"message","channel":"C_TEST","user":"U_HUMAN","text":"","ts":"1700000000.000001","thread_ts":"1699999999.000002"}""", "Human"];
        yield return ["legacy-bot-no-user", """{"type":"message","channel":"C_TEST","subtype":"bot_message"}""", "Bot"];
        yield return ["modern-bot-id-human-user", """{"type":"message","channel":"C_TEST","bot_id":"B_TEST","user":"U_HUMAN"}""", "Bot"];
        yield return ["modern-empty-bot-profile", """{"type":"message","channel":"C_TEST","bot_profile":{}}""", "Bot"];
        yield return ["self-without-human-fields", """{"type":"message","channel":"C_TEST","user":"U_SELF"}""", "Self"];
        yield return ["bot-before-self", """{"type":"message","channel":"C_TEST","user":"U_SELF","bot_id":"B_TEST"}""", "Bot"];
        yield return ["changed-without-outer-user-text", """{"type":"message","channel":"C_TEST","subtype":"message_changed","message":{"type":"message","ts":"1700000000.000001"}}""", "Edit"];
        yield return ["delete-without-user-text", """{"type":"message","channel":"C_TEST","subtype":"message_deleted","deleted_ts":"1700000000.000001"}""", "Delete"];
        yield return ["edited-bot-before-bot", """{"type":"message","channel":"C_TEST","user":"U_HUMAN","text":"edited","ts":"1700000000.000001","edited":{"ts":"1700000001.000002"},"bot_id":"B_TEST"}""", "Edit"];
        yield return ["edited-self-before-self", """{"type":"message","channel":"C_TEST","user":"U_SELF","text":"edited","ts":"1700000000.000001","edited":{"ts":"1700000001.000002"}}""", "Edit"];
        yield return ["unsupported-subtype-no-human-fields", """{"type":"message","channel":"C_TEST","subtype":"channel_join"}""", "UnsupportedMessageSubtype"];
    }

    [Theory]
    [MemberData(nameof(SupportedShapes))]
    public void ClassifiesProviderShapesWithoutManufacturingHumanFields(string caseId, string message, string expectedKind)
    {
        var frame = SocketModeTestData.Parse(SocketModeTestData.Envelope(message));
        Assert.Equal(SlackSocketFrameKind.Event, frame.Kind);
        Assert.Equal("envelope-a", frame.EnvelopeId);
        Assert.NotNull(frame.Event);
        Assert.Equal(expectedKind, frame.Event.Kind);
        Assert.Equal("Ev_TEST", frame.Event.ProviderEventId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000100), frame.Event.OccurredAt);
        Assert.Equal("C_TEST", frame.Event.ChannelId);
    }

    public static IEnumerable<object[]> MalformedShapes()
    {
        foreach (var field in new[] { "user", "text", "ts" })
        {
            yield return ["human-missing-" + field, SocketModeTestData.Change(SocketModeTestData.Human, x => x.Remove(field))];
        }
        foreach (var (name, value) in new[] { ("subtype", "null"), ("subtype", "\"\""), ("bot_id", "null"),
                     ("bot_id", "\"\""), ("bot_id", "12"), ("bot_profile", "null"), ("bot_profile", "[]"),
                     ("text", "null"), ("user", "17"), ("ts", "1700000000.1"), ("thread_ts", "null"),
                     ("event_ts", "\"invalid\""), ("hidden", "null"), ("hidden", "true"), ("edited", "null") })
        {
            yield return ["human-poison-" + name + "-" + value, SocketModeTestData.Change(SocketModeTestData.Human,
                x => x[name] = System.Text.Json.Nodes.JsonNode.Parse(value))];
        }
        yield return ["changed-missing-message", """{"type":"message","channel":"C_TEST","subtype":"message_changed","bot_id":"B_TEST"}"""];
        yield return ["changed-missing-nested-ts", """{"type":"message","channel":"C_TEST","subtype":"message_changed","message":{"type":"message"}}"""];
        yield return ["changed-wrong-nested-type", """{"type":"message","channel":"C_TEST","subtype":"message_changed","message":{"type":"file","ts":"1700000000.000001"}}"""];
        yield return ["delete-missing-deleted-ts", """{"type":"message","channel":"C_TEST","subtype":"message_deleted","bot_id":"B_TEST"}"""];
        yield return ["delete-conflicting-edit", """{"type":"message","channel":"C_TEST","subtype":"message_deleted","deleted_ts":"1700000000.000001","edited":{"ts":"1700000000.000001"}}"""];
        yield return ["changed-conflicting-delete", """{"type":"message","channel":"C_TEST","subtype":"message_changed","deleted_ts":"1700000000.000001","message":{"type":"message","ts":"1700000000.000001"}}"""];
        yield return ["unknown-subtype-nested-structural-form", """{"type":"message","channel":"C_TEST","subtype":"other","message":{}}"""];
        var edited = """{"type":"message","channel":"C_TEST","user":"U_SELF","text":"edited","ts":"1700000000.000001","edited":{"ts":"1700000001.000002"},"bot_id":"B_TEST"}""";
        foreach (var field in new[] { "user", "text", "ts" })
        {
            yield return ["edited-no-fallback-missing-" + field, SocketModeTestData.Change(edited, x => x.Remove(field))];
        }
        yield return ["edited-no-fallback-missing-edited-ts", SocketModeTestData.Change(edited, x => x["edited"]!.AsObject().Remove("ts"))];
        yield return ["edited-no-fallback-poisoned-bot", SocketModeTestData.Change(edited, x => x["bot_id"] = null)];
        yield return ["nested-optional-null-user", """{"type":"message","channel":"C_TEST","subtype":"message_changed","message":{"type":"message","ts":"1700000000.000001","user":null}}"""];
        yield return ["delete-optional-wrong-text", """{"type":"message","channel":"C_TEST","subtype":"message_deleted","deleted_ts":"1700000000.000001","text":false}"""];
    }

    [Theory]
    [MemberData(nameof(MalformedShapes))]
    public void RejectsMalformedRecognizedFieldsInsteadOfFallingBack(string caseId, string message) =>
        AssertWireRejected(SocketModeTestData.Envelope(message));

    [Theory]
    [InlineData("missing-channel")]
    [InlineData("wrong-channel")]
    [InlineData("wrong-channel-type")]
    [InlineData("wrong-event-type")]
    [InlineData("missing-event-id")]
    [InlineData("wrong-app")]
    [InlineData("wrong-team")]
    [InlineData("unexpected-enterprise")]
    [InlineData("fractional-event-time")]
    [InlineData("string-event-time")]
    [InlineData("zero-event-time")]
    [InlineData("missing-event-time")]
    [InlineData("missing-envelope")]
    [InlineData("interactive-envelope")]
    public void RejectsUntrustedOrAmbiguousRoutingBeforeClassification(string mutation)
    {
        var json = SocketModeTestData.Change(SocketModeTestData.Envelope(), root =>
        {
            var callback = root["payload"]!.AsObject();
            var message = callback["event"]!.AsObject();
            switch (mutation)
            {
                case "missing-channel": message.Remove("channel"); break;
                case "wrong-channel": message["channel"] = "C_OTHER"; break;
                case "wrong-channel-type": message["channel_type"] = "im"; break;
                case "wrong-event-type": message["type"] = "reaction_added"; break;
                case "missing-event-id": callback.Remove("event_id"); break;
                case "wrong-app": callback["api_app_id"] = "A_OTHER"; break;
                case "wrong-team": callback["team_id"] = "T_OTHER"; break;
                case "unexpected-enterprise": callback["enterprise_id"] = "E_OTHER"; break;
                case "fractional-event-time": callback["event_time"] = 1700000100.5; break;
                case "string-event-time": callback["event_time"] = "1700000100"; break;
                case "zero-event-time": callback["event_time"] = 0; break;
                case "missing-event-time": callback.Remove("event_time"); break;
                case "missing-envelope": root.Remove("envelope_id"); break;
                case "interactive-envelope": root["type"] = "interactive"; break;
                default: throw new ArgumentOutOfRangeException(nameof(mutation));
            }
        });
        AssertWireRejected(json);
    }

    [Theory]
    [InlineData("hello", null, "Hello")]
    [InlineData("disconnect", "warning", "Refresh")]
    [InlineData("disconnect", "refresh_requested", "Refresh")]
    [InlineData("disconnect", "link_disabled", "LinkDisabled")]
    public void RecognizedControlsNeverAcquireAnEnvelopeOrEvent(string type, string? reason, string kind)
    {
        var frame = SocketModeTestData.Parse(JsonSerializer.Serialize(new { type, reason }));
        Assert.Equal(kind, frame.Kind.ToString());
        Assert.Null(frame.EnvelopeId);
        Assert.Null(frame.Event);
    }

    [Theory]
    [InlineData("""{"type":"disconnect","reason":"refresh"}""")]
    [InlineData("""{"type":"disconnect","reason":null}""")]
    [InlineData("""{"type":"hello","envelope_id":"E_TEST"}""")]
    [InlineData("""{"type":"disconnect","reason":"warning","envelope_id":"E_TEST"}""")]
    [InlineData("""{"type":"unknown"}""")]
    public void UnknownOrEnvelopedControlsAreRejected(string json) => AssertWireRejected(json);

    [Theory]
    [InlineData("\"type\":\"events_api\"", "\"type\":\"events_api\",\"type\":\"hello\"")]
    [InlineData("\"event_id\":\"Ev_TEST\"", "\"event_id\":\"Ev_TEST\",\"event_id\":\"Ev_OTHER\"")]
    [InlineData("\"text\":\"hello\"", "\"text\":\"hello\",\"text\":\"other\"")]
    public void RawDuplicateKeysAreRejectedAtEveryAuthorityLayer(string from, string to) =>
        AssertWireRejected(SocketModeTestData.Envelope().Replace(from, to, StringComparison.Ordinal));

    [Fact]
    public void InvalidUtf8IsRejectedWithOnlyTheFixedDiagnostic()
    {
        // Replacement decoding would leave valid JSON, so only strict UTF-8 rejects this message.
        var json = SocketModeTestData.Envelope();
        var bytes = Encoding.UTF8.GetBytes(json);
        var textOffset = Encoding.UTF8.GetByteCount(json[..json.IndexOf("hello", StringComparison.Ordinal)]);
        bytes[textOffset] = 0xc3;
        bytes[textOffset + 1] = 0x28;
        var error = Assert.Throws<InvalidDataException>(() => new SlackSocketWireParser(SocketModeTestData.Configuration()).Parse(bytes));
        Assert.Equal("Socket frame failed the supported wire contract.", error.Message);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public void EnvelopeAndNormalizedPayloadLimitsAreBothEnforced()
    {
        var envelope = SocketModeTestData.Envelope();
        var length = Encoding.UTF8.GetByteCount(envelope);
        Assert.NotNull(SocketModeTestData.Parse(envelope, SocketModeTestData.Configuration(SocketModeTestData.Limits with { MaximumEnvelopeBytes = length })).Event);
        AssertWireRejected(envelope, SocketModeTestData.Configuration(SocketModeTestData.Limits with { MaximumEnvelopeBytes = length - 1 }));
        var subscription = SocketModeTestData.Subscription();
        subscription = subscription with { Configuration = subscription.Configuration with { Policy = subscription.Configuration.Policy with { MaximumPayloadBytes = 128 } } };
        AssertWireRejected(envelope, SocketModeTestData.Configuration(subscriptions: [subscription]));
    }

    [Fact]
    public void NestedUnknownDataStillObeysDepthAndDuplicateKeyGuards()
    {
        var message = SocketModeTestData.Human.Replace("\"text\":\"hello\"", "\"text\":\"hello\",\"unknown\":{\"a\":{\"b\":{\"c\":1}}}", StringComparison.Ordinal);
        AssertWireRejected(SocketModeTestData.Envelope(message), SocketModeTestData.Configuration(SocketModeTestData.Limits with { MaximumJsonDepth = 4 }));
        AssertWireRejected(SocketModeTestData.Envelope(SocketModeTestData.Human.Replace("\"text\":\"hello\"", "\"text\":\"hello\",\"unknown\":{\"x\":1,\"x\":2}", StringComparison.Ordinal)));
    }

    private static void AssertWireRejected(string json, SlackSocketModeConfiguration? configuration = null)
    {
        var error = Assert.Throws<InvalidDataException>(() => SocketModeTestData.Parse(json, configuration));
        Assert.Equal("Socket frame failed the supported wire contract.", error.Message);
        Assert.Null(error.InnerException);
    }
}
