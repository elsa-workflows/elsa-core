using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Serialization;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Reads the workflow-level log persistence mode stored on a workflow definition.
/// </summary>
internal static class WorkflowLogPersistenceDisplay
{
    /// <summary>
    /// Returns the mode name (<c>Include</c>, <c>Exclude</c>, <c>Inherit</c>, a custom strategy name)
    /// or the expression that selects the mode. An unset workflow inherits its parent configuration.
    /// </summary>
    public static string Format(IDictionary<string, object>? customProperties)
    {
        if (customProperties != null &&
            TryRead(customProperties, "logPersistenceConfig", out var config) &&
            TryRead(config, "default", out var defaultConfig))
            return FormatConfiguration(defaultConfig) ?? nameof(LogPersistenceMode.Inherit);

        if (customProperties != null &&
            TryRead(customProperties, "logPersistenceMode", out var legacy) &&
            TryRead(legacy, "default", out var legacyDefault))
        {
            var mode = FormatLegacy(legacyDefault);
            if (!string.IsNullOrWhiteSpace(mode))
                return mode;
        }

        return nameof(LogPersistenceMode.Inherit);
    }

    private static string? FormatConfiguration(object value)
    {
        try
        {
            var json = ToJson(value);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var config = JsonSerializer.Deserialize<LogPersistenceConfiguration>(json, SerializerOptions.LogPersistenceConfigSerializerOptions);
            if (config == null)
                return null;

            if (config.EvaluationMode == LogPersistenceEvaluationMode.Expression)
            {
                var expression = ReadExpression(config.Expression);
                if (!string.IsNullOrWhiteSpace(expression))
                    return expression.Trim();
            }

            return ShortName(config.StrategyType) ?? nameof(LogPersistenceMode.Inherit);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            // A definition with an unexpected payload must still open in the instance viewer.
            return null;
        }
    }

    private static string? FormatLegacy(object value)
    {
        switch (value)
        {
            case JsonElement { ValueKind: JsonValueKind.String } element:
                return NormalizeMode(element.GetString());
            case JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt32(out var number):
                return ModeFromNumber(number);
            case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text):
                return NormalizeMode(text);
            case JsonValue jsonValue when jsonValue.TryGetValue<int>(out var number):
                return ModeFromNumber(number);
            case string text:
                return NormalizeMode(text);
            case LogPersistenceMode mode:
                return mode.ToString();
            case int number:
                return ModeFromNumber(number);
            case long number when number is >= int.MinValue and <= int.MaxValue:
                return ModeFromNumber((int)number);
            default:
                return null;
        }
    }

    private static string? ReadExpression(Expression? expression)
    {
        if (expression?.Value == null)
            return null;

        return expression.Value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement element => element.GetRawText(),
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
            _ => expression.Value.ToString()
        };
    }

    private static string? NormalizeMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Contains('.') ? ShortName(trimmed) : trimmed;
    }

    private static string? ModeFromNumber(int number) => number switch
    {
        0 => nameof(LogPersistenceMode.Inherit),
        1 => nameof(LogPersistenceMode.Include),
        2 => nameof(LogPersistenceMode.Exclude),
        _ => null
    };

    private static string? ShortName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var type = typeName.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        var name = type.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return name.Length == 0 ? null : name[^1];
    }

    private static bool TryRead(object? source, string name, out object value)
    {
        value = null!;
        switch (source)
        {
            case JsonElement element when element.ValueKind == JsonValueKind.Object &&
                                           element.TryGetProperty(name, out var property) &&
                                           property.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined:
                value = property;
                return true;
            case JsonObject obj when obj.TryGetPropertyValue(name, out var node) && node != null:
                value = node;
                return true;
            case IDictionary<string, object> dictionary when dictionary.TryGetValue(name, out var entry) && IsPresent(entry):
                value = entry!;
                return true;
            case IDictionary<string, object?> nullableDictionary when nullableDictionary.TryGetValue(name, out var entry) && IsPresent(entry):
                value = entry!;
                return true;
            default:
                return false;
        }
    }

    private static bool IsPresent(object? value) => value switch
    {
        null => false,
        JsonElement element => element.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined,
        _ => true
    };

    private static string? ToJson(object value) => value switch
    {
        JsonElement element => element.GetRawText(),
        JsonNode node => node.ToJsonString(),
        _ => JsonSerializer.Serialize(value)
    };
}
