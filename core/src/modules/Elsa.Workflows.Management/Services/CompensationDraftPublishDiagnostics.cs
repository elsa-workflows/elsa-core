using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Services;

[PublicAPI]
public static class CompensationDraftPublishDiagnostics
{
    public const string Code = CompensationScope.DraftOnlyDiagnosticCode;

    /// <summary>
    /// Finds draft-only compensation scopes in a materialized workflow graph.
    /// </summary>
    public static IReadOnlyCollection<WorkflowValidationError> Create(WorkflowGraph graph)
    {
        var errors = new List<WorkflowValidationError>();
        foreach (var node in graph.Nodes)
        {
            var activity = node.Activity;
            if (activity is CompensationScope scope)
            {
                AddScopeDiagnostics(errors, scope.Id, scope.Steps.Select(ToStepData));
                continue;
            }

            if (activity is NotFoundActivity notFound)
            {
                var hasOriginalScope = TryReadOpaqueScope(notFound.OriginalActivityJson, out var data);
                if (string.Equals(notFound.MissingTypeName, CompensationScope.ActivityTypeName, StringComparison.Ordinal) || hasOriginalScope)
                    AddScopeDiagnostics(errors, notFound.Id, hasOriginalScope ? data : Array.Empty<StepDiagnosticData>());
            }
        }

        return errors;
    }

    private static void AddScopeDiagnostics(ICollection<WorkflowValidationError> errors, string? scopeId, IEnumerable<StepDiagnosticData> steps)
    {
        var stepList = steps.ToList();
        if (stepList.Count == 0)
            stepList.Add(new(null, null, null, [], [], false));

        foreach (var step in stepList)
        {
            AddError(errors, scopeId, step.StepId, "scope", step.ActivityIds, step.MappingIds, null);
            if (!step.HasRow)
                continue;

            AddError(errors, scopeId, step.StepId, "forward", [step.ForwardActivityId], [], null);
            AddError(errors, scopeId, step.StepId, "handler", [step.HandlerActivityId], [], null);

            foreach (var mappingId in step.MappingIds)
                AddError(errors, scopeId, step.StepId, "mapping", step.ActivityIds, [mappingId], mappingId);
        }
    }

    private static void AddError(ICollection<WorkflowValidationError> errors, string? scopeId, string? stepId, string role, IEnumerable<string?> relatedActivityIds, IEnumerable<string?> mappingIds, string? mappingId)
    {
        var data = new Dictionary<string, object?>
        {
            ["scopeActivityId"] = scopeId,
            ["stepId"] = stepId,
            ["role"] = role,
            ["relatedActivityIds"] = relatedActivityIds.Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray(),
            ["mappingIds"] = mappingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray()
        };
        if (mappingId != null)
            data["mappingId"] = mappingId;

        errors.Add(new(CompensationScope.DraftOnlyRefusalMessage, scopeId, Code, data));
    }

    private static StepDiagnosticData ToStepData(CompensationStep step) => new(
        step.StepId,
        step.Forward?.Id,
        step.Handler?.Id,
        new[] { step.Forward?.Id, step.Handler?.Id }.Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray(),
        step.Mappings.Select(ReadMappingId).Where(x => x != null).Cast<string>().ToArray());

    private static bool TryReadOpaqueScope(string? json, out IEnumerable<StepDiagnosticData> steps)
    {
        steps = [];
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String ||
                !string.Equals(type.GetString(), CompensationScope.ActivityTypeName, StringComparison.Ordinal))
                return false;

            if (!root.TryGetProperty("steps", out var rawSteps) || rawSteps.ValueKind != JsonValueKind.Array)
                return true;

            steps = rawSteps.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Object)
                .Select(x => new StepDiagnosticData(
                    ReadString(x, "stepId"),
                    ReadActivityId(x, "forward"),
                    ReadActivityId(x, "handler"),
                    new[] { ReadActivityId(x, "forward"), ReadActivityId(x, "handler") }.Where(y => y != null).Cast<string>().ToArray(),
                    ReadMappingIds(x),
                    true))
                .ToArray();
            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static string[] ReadMappingIds(JsonElement step)
    {
        if (!step.TryGetProperty("mappings", out var mappings) || mappings.ValueKind != JsonValueKind.Array)
            return [];

        return mappings.EnumerateArray().Select(ReadMappingId).Where(x => x != null).Cast<string>().ToArray();
    }

    private static string? ReadActivityId(JsonElement step, string propertyName) =>
        step.TryGetProperty(propertyName, out var activity) && activity.ValueKind == JsonValueKind.Object
            ? ReadString(activity, "id")
            : null;

    private static string? ReadMappingId(JsonElement mapping) =>
        mapping.ValueKind == JsonValueKind.Object ? ReadString(mapping, "mappingId") : null;

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private sealed record StepDiagnosticData(string? StepId, string? ForwardActivityId, string? HandlerActivityId, string[] ActivityIds, string[] MappingIds, bool HasRow = true);
}
