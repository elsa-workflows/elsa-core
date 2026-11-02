using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Serialization;
using Elsa.Api.Client.Shared.Enums;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Workflow-level log persistence as shown in the instance viewer.
/// Expression source stays untranslated; only named modes are localized.
/// </summary>
internal readonly record struct LogPersistenceDisplay(bool IsExpression, string Text);

/// <summary>
/// Reads the workflow-level log persistence value stored on a workflow definition.
/// Definition custom properties are <see cref="JsonElement"/> values.
/// </summary>
internal static class WorkflowLogPersistenceDisplay
{
    /// <summary>
    /// Returns the configured mode or expression. An unset workflow inherits.
    /// A blank expression is <see cref="LogPersistenceMode.Inherit"/>; expression mode does not read <c>strategyType</c>.
    /// </summary>
    public static LogPersistenceDisplay Format(IDictionary<string, object>? customProperties)
    {
        if (TryReadDefault(customProperties, "logPersistenceConfig", out var configured))
            return ReadConfiguration(configured);

        if (TryReadDefault(customProperties, "logPersistenceMode", out var legacy) && ReadLegacy(legacy) is { } mode)
            return Mode(mode);

        return Mode(nameof(LogPersistenceMode.Inherit));
    }

    private static LogPersistenceDisplay Mode(string text) => new(false, text);

    private static LogPersistenceDisplay Expression(string text) => new(true, text);

    private static LogPersistenceDisplay ReadConfiguration(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
            return Mode(nameof(LogPersistenceMode.Inherit));

        try
        {
            var config = JsonSerializer.Deserialize<LogPersistenceConfiguration>(value.GetRawText(), SerializerOptions.LogPersistenceConfigSerializerOptions);
            if (config == null)
                return Mode(nameof(LogPersistenceMode.Inherit));

            if (config.EvaluationMode == LogPersistenceEvaluationMode.Expression)
            {
                var expression = ExpressionText(config.Expression?.Value);
                return string.IsNullOrWhiteSpace(expression)
                    ? Mode(nameof(LogPersistenceMode.Inherit))
                    : Expression(expression.Trim());
            }

            return Mode(ShortName(config.StrategyType) ?? nameof(LogPersistenceMode.Inherit));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            // A definition with an unexpected payload must still open in the instance viewer.
            return Mode(nameof(LogPersistenceMode.Inherit));
        }
    }

    private static string? ReadLegacy(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return NormalizeMode(value.GetString());

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return ModeFromNumber(number);

        return null;
    }

    private static string? ExpressionText(object? value) => value switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.GetRawText(),
        _ => null
    };

    private static string? NormalizeMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Contains('.') ? ShortName(trimmed) : trimmed;
    }

    private static string? ModeFromNumber(int number) => number switch
    {
        (int)LogPersistenceMode.Inherit => nameof(LogPersistenceMode.Inherit),
        (int)LogPersistenceMode.Include => nameof(LogPersistenceMode.Include),
        (int)LogPersistenceMode.Exclude => nameof(LogPersistenceMode.Exclude),
        _ => null
    };

    private static string? ShortName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        // Keep the empty segment in ", Elsa.Workflows.Core" so it is not treated as a type name.
        var type = typeName.Split(',', 2, StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(type))
            return null;

        var name = type.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return name.Length == 0 ? null : name[^1];
    }

    /// <summary>
    /// Reads <c>properties[key].default</c> when that value is present.
    /// A null or missing default falls through to the next source.
    /// </summary>
    private static bool TryReadDefault(IDictionary<string, object>? properties, string key, out JsonElement value)
    {
        value = default;
        if (properties == null ||
            !properties.TryGetValue(key, out var entry) ||
            entry is not JsonElement { ValueKind: JsonValueKind.Object } element ||
            !element.TryGetProperty("default", out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        value = property;
        return true;
    }
}
