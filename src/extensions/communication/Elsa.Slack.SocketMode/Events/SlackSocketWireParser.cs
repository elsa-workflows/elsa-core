using System.Text;
using System.Text.Json;

namespace Elsa.Slack.SocketMode.Events;

internal enum SlackSocketFrameKind { Event, Hello, Refresh, LinkDisabled }
internal sealed record SlackSocketFrame(SlackSocketFrameKind Kind, string? EnvelopeId, SlackSocketEventPayload? Event);

/// <summary>Validates the bounded provider wire shape before SDK deserialization or durable admission.</summary>
internal sealed class SlackSocketWireParser(SlackSocketModeConfiguration configuration)
{
    internal SlackSocketFrame Parse(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = SlackSocketJson.Parse(bytes, configuration.Limits.MaximumEnvelopeBytes, configuration.Limits.MaximumJsonDepth);
            var root = document.RootElement;
            DemandObject(root);
            var type = RequiredString(root, "type");
            if (type == "hello")
            {
                Demand(!root.TryGetProperty("envelope_id", out _));
                return new(SlackSocketFrameKind.Hello, null, null);
            }
            if (type == "disconnect")
            {
                Demand(!root.TryGetProperty("envelope_id", out _));
                var reason = RequiredString(root, "reason");
                return reason switch
                {
                    "warning" or "refresh_requested" => new(SlackSocketFrameKind.Refresh, null, null),
                    "link_disabled" => new(SlackSocketFrameKind.LinkDisabled, null, null),
                    _ => throw new InvalidDataException("Unrecognized Socket control.")
                };
            }
            Demand(type == "events_api");
            var envelopeId = RequiredIdentifier(root, "envelope_id");
            var callback = RequiredObject(root, "payload");
            Demand(RequiredString(callback, "type") == "event_callback");
            Demand(RequiredIdentifier(callback, "api_app_id") == configuration.ExpectedAppId);
            ValidateInstallation(callback, "team_id", configuration.ExpectedTeamId);
            ValidateInstallation(callback, "enterprise_id", configuration.ExpectedEnterpriseId);
            var eventId = RequiredIdentifier(callback, "event_id");
            var time = callback.GetProperty("event_time");
            Demand(time.ValueKind == JsonValueKind.Number && time.TryGetInt64(out _));
            var spelling = time.GetRawText();
            Demand(spelling.Length > 0 && spelling.All(x => x is >= '0' and <= '9'));
            var occurredAt = DateTimeOffset.FromUnixTimeSeconds(time.GetInt64());
            Demand(occurredAt > DateTimeOffset.UnixEpoch);
            var message = RequiredObject(callback, "event");
            Demand(RequiredString(message, "type") == "message");
            Demand(RequiredIdentifier(message, "channel") == configuration.ChannelId);
            if (message.TryGetProperty("channel_type", out var channelType))
            {
                Demand(channelType.ValueKind == JsonValueKind.String && channelType.GetString() == "channel");
            }
            ValidateKnownFields(message);
            var kind = Classify(message);
            var projection = SlackSocketEventPayload.ProjectionPaths.Select(path => Project(message, path)).ToArray();
            var payload = new SlackSocketEventPayload(1, configuration.BindingFingerprint, SlackSocketJson.Digest(message), kind.ToString(),
                configuration.ExpectedAppId, configuration.ExpectedTeamId, configuration.ExpectedEnterpriseId, eventId, occurredAt,
                configuration.ChannelId, configuration.SelfUserId, Array.AsReadOnly(projection));
            var normalized = JsonSerializer.Serialize(payload);
            Demand(configuration.Subscriptions.All(x => Encoding.UTF8.GetByteCount(normalized) <= x.Configuration.Policy.MaximumPayloadBytes &&
                Encoding.UTF8.GetByteCount(eventId) <= x.Configuration.Policy.MaximumProviderEventIdBytes));
            return new(SlackSocketFrameKind.Event, envelopeId, payload);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or
                                         InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            // Do not retain the original exception, JSON location, provider values or URL/token-bearing data.
            throw new InvalidDataException("Socket frame failed the supported wire contract.");
        }
    }

    private SlackSocketEventKind Classify(JsonElement message)
    {
        var subtype = OptionalString(message, "subtype");
        var edited = message.TryGetProperty("edited", out _);
        var nested = message.TryGetProperty("message", out _);
        var deleted = message.TryGetProperty("deleted_ts", out _);
        if (subtype == "message_changed")
        {
            Demand(!deleted && !edited);
            var changed = RequiredObject(message, "message");
            Demand(RequiredString(changed, "type") == "message");
            ValidateTimestamp(RequiredString(changed, "ts"));
            return SlackSocketEventKind.Edit;
        }
        if (subtype == "message_deleted")
        {
            Demand(!edited && !nested);
            ValidateTimestamp(RequiredString(message, "deleted_ts"));
            return SlackSocketEventKind.Delete;
        }
        // A structural field cannot hide behind a bot marker or unsupported-subtype fallback.
        Demand(!nested && !deleted);
        if (edited)
        {
            Demand(subtype is null);
            _ = RequiredIdentifier(message, "user");
            _ = RequiredString(message, "text");
            ValidateTimestamp(RequiredString(message, "ts"));
            ValidateTimestamp(RequiredString(RequiredObject(message, "edited"), "ts"));
            return SlackSocketEventKind.Edit;
        }
        if (subtype == "bot_message" || message.TryGetProperty("bot_id", out _) || message.TryGetProperty("bot_profile", out _))
        {
            return SlackSocketEventKind.Bot;
        }
        if (OptionalString(message, "user") == configuration.SelfUserId)
        {
            return SlackSocketEventKind.Self;
        }
        if (subtype is not null)
        {
            return SlackSocketEventKind.UnsupportedMessageSubtype;
        }
        _ = RequiredIdentifier(message, "user");
        _ = RequiredString(message, "text");
        ValidateTimestamp(RequiredString(message, "ts"));
        Demand(!message.TryGetProperty("hidden", out var hidden) || hidden.ValueKind == JsonValueKind.False);
        return SlackSocketEventKind.Human;
    }

    private static void ValidateKnownFields(JsonElement message)
    {
        foreach (var name in new[] { "user", "bot_id", "subtype", "type", "channel", "channel_type" })
        {
            if (message.TryGetProperty(name, out _))
            {
                _ = RequiredIdentifier(message, name);
            }
        }
        if (message.TryGetProperty("text", out _))
        {
            _ = RequiredString(message, "text");
        }
        foreach (var name in new[] { "ts", "thread_ts", "event_ts", "deleted_ts" })
        {
            if (message.TryGetProperty(name, out _))
            {
                ValidateTimestamp(RequiredString(message, name));
            }
        }
        if (message.TryGetProperty("hidden", out var hidden))
        {
            Demand(hidden.ValueKind is JsonValueKind.True or JsonValueKind.False);
        }
        if (message.TryGetProperty("bot_profile", out var profile))
        {
            DemandObject(profile);
        }
        if (message.TryGetProperty("edited", out var edited))
        {
            DemandObject(edited);
            ValidateTimestamp(RequiredString(edited, "ts"));
            if (edited.TryGetProperty("user", out _))
            {
                _ = RequiredIdentifier(edited, "user");
            }
        }
        foreach (var name in new[] { "message", "previous_message" })
        {
            if (message.TryGetProperty(name, out var nested))
            {
                DemandObject(nested);
                ValidateKnownFields(nested);
            }
        }
    }

    private static SlackSocketProjectionValue Project(JsonElement message, string path)
    {
        var value = message;
        foreach (var part in path.Split('.'))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value))
            {
                return new(path, false, JsonValueKind.Undefined, null);
            }
        }
        return new(path, true, value.ValueKind,
            value.ValueKind == JsonValueKind.String ? value.GetString() : SlackSocketJson.Canonicalize(value));
    }

    private static void ValidateInstallation(JsonElement callback, string property, string? expected)
    {
        if (expected is null)
        {
            Demand(!callback.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null);
            return;
        }
        Demand(RequiredIdentifier(callback, property) == expected);
    }

    private static string RequiredIdentifier(JsonElement value, string name)
    {
        var result = RequiredString(value, name);
        SlackSocketModeConfiguration.ValidateIdentifier(result);
        return result;
    }

    private static string RequiredString(JsonElement value, string name)
    {
        var property = value.GetProperty(name);
        Demand(property.ValueKind == JsonValueKind.String);
        return property.GetString()!;
    }

    private static string? OptionalString(JsonElement value, string name) => value.TryGetProperty(name, out _) ? RequiredString(value, name) : null;
    private static JsonElement RequiredObject(JsonElement value, string name)
    {
        var result = value.GetProperty(name);
        DemandObject(result);
        return result;
    }

    internal static void ValidateTimestamp(string value)
    {
        var separator = value.IndexOf('.');
        Demand(value.Length <= 256 && separator > 0 && separator < value.Length - 1 &&
               value.IndexOf('.', separator + 1) == -1 && value.Where((_, i) => i != separator).All(x => x is >= '0' and <= '9'));
    }

    private static void DemandObject(JsonElement value) => Demand(value.ValueKind == JsonValueKind.Object);
    private static void Demand(bool condition)
    {
        if (!condition)
        {
            throw new InvalidDataException("Socket wire value is invalid.");
        }
    }
}
