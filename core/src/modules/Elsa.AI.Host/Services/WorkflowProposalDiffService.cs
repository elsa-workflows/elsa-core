using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class WorkflowProposalDiffService
{
    public AIGraphDiff CreateDiff(JsonObject draft, JsonObject? baseline = null)
    {
        var baselineIds = GetActivityIds(baseline).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var draftIds = GetActivityIds(draft).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AIGraphDiff
        {
            AddedActivityIds = draftIds.Except(baselineIds, StringComparer.OrdinalIgnoreCase).Order().ToList(),
            RemovedActivityIds = baselineIds.Except(draftIds, StringComparer.OrdinalIgnoreCase).Order().ToList(),
            ChangedActivityIds = [],
            Data = new JsonObject
            {
                ["baselineActivityCount"] = baselineIds.Count,
                ["draftActivityCount"] = draftIds.Count
            }
        };
    }

    private static IEnumerable<string> GetActivityIds(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            var type = ReadString(jsonObject, "type") ?? ReadString(jsonObject, "typeName") ?? ReadString(jsonObject, "activityType");
            var id = ReadString(jsonObject, "id") ?? ReadString(jsonObject, "activityId") ?? ReadString(jsonObject, "nodeId");
            if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(id))
                yield return id;

            foreach (var child in jsonObject.Select(x => x.Value))
            {
                foreach (var childId in GetActivityIds(child))
                    yield return childId;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                foreach (var childId in GetActivityIds(child))
                    yield return childId;
            }
        }
    }

    private static string? ReadString(JsonObject jsonObject, string name) =>
        jsonObject.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}
