using Elsa.AI.Abstractions.Models;
using Elsa.Workflows;

namespace Elsa.AI.Host.Services;

public class WorkflowDraftValidationService(IServiceProvider serviceProvider)
{
    public IReadOnlyCollection<AIValidationDiagnostic> Validate(JsonObject draft, string? baselineVersionId = null, string? expectedBaselineVersionId = null)
    {
        var diagnostics = new List<AIValidationDiagnostic>();
        if (draft.Count == 0)
            diagnostics.Add(Error("draft.empty", "Draft payload is empty.", "$"));

        if (!string.IsNullOrWhiteSpace(expectedBaselineVersionId) &&
            !string.Equals(baselineVersionId, expectedBaselineVersionId, StringComparison.Ordinal))
            diagnostics.Add(Error("baseline.stale", "The proposal baseline version is stale.", "$.baselineVersionId"));

        var registry = serviceProvider.GetService(typeof(IActivityRegistry)) as IActivityRegistry;
        if (registry == null)
        {
            diagnostics.Add(Warning("activityRegistry.unavailable", "Activity Registry is not available, so activity validation was skipped.", "$"));
            return diagnostics;
        }

        foreach (var activityType in FindActivityTypes(draft).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (registry.Find(activityType) == null)
                diagnostics.Add(Error("activity.unavailable", $"Activity '{activityType}' is not installed.", "$"));
        }

        return diagnostics;
    }

    private static IEnumerable<string> FindActivityTypes(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            if (TryReadString(jsonObject, "type", out var type) || TryReadString(jsonObject, "typeName", out type) || TryReadString(jsonObject, "activityType", out type))
                yield return type;

            foreach (var child in jsonObject.Select(x => x.Value))
            {
                foreach (var activityType in FindActivityTypes(child))
                    yield return activityType;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                foreach (var activityType in FindActivityTypes(child))
                    yield return activityType;
            }
        }
    }

    private static bool TryReadString(JsonObject jsonObject, string name, out string value)
    {
        value = "";
        if (!jsonObject.TryGetPropertyValue(name, out var node) || node is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var result) || string.IsNullOrWhiteSpace(result))
            return false;

        value = result;
        return true;
    }

    private static AIValidationDiagnostic Error(string code, string message, string path) =>
        new() { Code = code, Message = message, Path = path, Severity = AIValidationSeverity.Error };

    private static AIValidationDiagnostic Warning(string code, string message, string path) =>
        new() { Code = code, Message = message, Path = path, Severity = AIValidationSeverity.Warning };
}
