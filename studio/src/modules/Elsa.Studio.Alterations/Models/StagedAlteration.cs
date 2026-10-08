using System.Text.Json.Nodes;
using Elsa.Studio.Alterations.Catalog;

namespace Elsa.Studio.Alterations.Models;

/// <summary>
/// A single in-memory entry in the staging panel. Holds enough state to render a meaningful
/// summary, edit it later, and emit the JSON object that the server-side alteration handler
/// expects when the user finally hits Submit.
/// </summary>
public class StagedAlteration
{
    /// <summary>Local identity for stable rendering / removal — not sent to the server.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The catalog descriptor this staged item is an instance of.</summary>
    public AlterationDescriptor Descriptor { get; init; } = null!;

    /// <summary>
    /// For Activity-target alterations: the activity's id within the workflow (matches `node.id`
    /// on the diagram). Null for Instance- or Variable-target alterations.
    /// </summary>
    public string? TargetActivityId { get; set; }

    /// <summary>Display name of the target activity (cached from the diagram for rendering).</summary>
    public string? TargetActivityDisplayName { get; set; }

    /// <summary>Configured field values, keyed by <see cref="AlterationFieldSpec.Key"/>.</summary>
    public Dictionary<string, string> ConfigValues { get; init; } = new();

    /// <summary>
    /// Builds the JSON object the server expects: <c>{ "type": ..., "activityId": ..., ...config }</c>.
    /// </summary>
    public JsonObject ToAlterationJson()
    {
        var obj = new JsonObject
        {
            ["type"] = Descriptor.TypeId,
        };

        if (Descriptor.Target == AlterationTargetKind.Activity && !string.IsNullOrWhiteSpace(TargetActivityId))
            obj["activityId"] = TargetActivityId;

        foreach (var (key, value) in ConfigValues)
        {
            obj[key] = ToJsonNode(key, value);
        }

        return obj;
    }

    private JsonNode? ToJsonNode(string key, string raw)
    {
        var spec = Descriptor.ConfigFields.FirstOrDefault(f => f.Key == key);
        switch (spec?.Kind)
        {
            case AlterationFieldKind.Integer:
                return long.TryParse(raw, out var n) ? JsonValue.Create(n) : JsonValue.Create(raw);
            case AlterationFieldKind.VersionPicker:
                return int.TryParse(raw, out var version) ? JsonValue.Create(version) : JsonValue.Create(raw);
            case AlterationFieldKind.Json:
                try { return JsonNode.Parse(raw); }
                catch { return JsonValue.Create(raw); }
            default:
                return JsonValue.Create(raw);
        }
    }

    /// <summary>One-line summary of the configured values, shown under the staging row.</summary>
    public string SummariseConfig()
    {
        if (ConfigValues.Count == 0) return string.Empty;
        return string.Join(", ", ConfigValues.Select(kv => $"{kv.Key}={Truncate(kv.Value, 30)}"));
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
