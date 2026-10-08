using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Events;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.Tests.SocketMode;

internal static class SocketModeTestData
{
    internal const string Human = """{"type":"message","channel":"C_TEST","user":"U_HUMAN","text":"hello","ts":"1700000000.000001"}""";

    internal static SlackSocketModeLimits Limits => new(65536, 16, TimeSpan.FromSeconds(5), 16,
        8, 8, 2, 4, 3, TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

    internal static SlackSocketSubscription Subscription(string id = "subscription-a") => new(
        new AdmissionSubscriptionConfiguration(id, "tenant", "environment", "installation", "C_TEST",
            "definition", "version-id", 1, new string('a', 64), DateTimeOffset.FromUnixTimeSeconds(1700000000),
            new AdmissionPolicy(TimeSpan.Zero, TimeSpan.FromDays(2), TimeSpan.FromDays(1), TimeSpan.FromMinutes(1),
                4, 8, 16384, 128, AdmissionRejectedEventDisposition.Reject, AdmissionRejectedEventDisposition.Quarantine,
                "fixture-cleanup")), 1);

    internal static SlackSocketModeConfiguration Configuration(SlackSocketModeLimits? limits = null,
        IReadOnlyList<SlackSocketSubscription>? subscriptions = null, string? teamId = "T_TEST", string? enterpriseId = null) =>
        new("tenant", "environment", "installation", "connection", "A_TEST", teamId, enterpriseId, "U_SELF", "C_TEST",
            subscriptions ?? [Subscription()], limits ?? Limits);

    // Raw insertion preserves duplicate properties and JSON number spelling for parser regressions.
    internal static string Envelope(string message = Human, string envelopeId = "envelope-a") =>
        "{\"type\":\"events_api\",\"envelope_id\":" + JsonSerializer.Serialize(envelopeId) +
        ",\"payload\":{\"type\":\"event_callback\",\"api_app_id\":\"A_TEST\",\"team_id\":\"T_TEST\",\"event_id\":\"Ev_TEST\",\"event_time\":1700000100,\"event\":" + message + "}}";

    internal static string Change(string json, Action<JsonObject> change)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        change(root);
        return root.ToJsonString();
    }

    internal static SlackSocketFrame Parse(string json, SlackSocketModeConfiguration? configuration = null) =>
        new SlackSocketWireParser(configuration ?? Configuration()).Parse(Encoding.UTF8.GetBytes(json));

    internal static SlackSocketEventPayload Event(string message = Human) => Parse(Envelope(message)).Event!;

    internal static JsonObject NormalizedHuman() => JsonNode.Parse(JsonSerializer.Serialize(Event()))!.AsObject();

    internal static JsonObject Slot(JsonObject payload, string path) => payload["Projection"]!.AsArray()
        .Select(x => x!.AsObject()).Single(x => x["Path"]!.GetValue<string>() == path);
}
