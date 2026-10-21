using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities.Compensation;

/// <summary>
/// Keeps every supported Core execution entry point closed while native compensation is draft-only.
/// </summary>
internal static class CompensationScopeExecutionGuard
{
    public static void DemandExecutable(WorkflowGraph workflowGraph)
    {
        if (CompensationScopeExecutionState.TryGetPass(workflowGraph, out var observation))
            return;

        if (!observation.IsEnabled)
        {
            DemandExecutableByScanning(workflowGraph);
            return;
        }

        if (!observation.NeedsClassification)
        {
            DemandExecutableByScanning(workflowGraph);

            if (observation.ActivitiesAreCacheable)
                CompensationScopeExecutionState.RecordCompletedScan(observation, activitiesAreCacheable: true);

            return;
        }

        var activitiesAreCacheable = true;
        foreach (var node in workflowGraph.Nodes)
        {
            if (activitiesAreCacheable && !CompensationScopeExecutionState.IsActivityTypeCacheable(node.Activity))
                activitiesAreCacheable = false;

            if (IsCompensationScope(node.Activity))
                throw new InvalidOperationException(CompensationScope.DraftOnlyRefusalMessage);
        }

        CompensationScopeExecutionState.RecordCompletedScan(observation, activitiesAreCacheable);
    }

    private static void DemandExecutableByScanning(WorkflowGraph workflowGraph)
    {
        if (workflowGraph.Nodes.Any(node => IsCompensationScope(node.Activity)))
            throw new InvalidOperationException(CompensationScope.DraftOnlyRefusalMessage);
    }

    public static void DemandExecutable(WorkflowExecutionContext workflowExecutionContext) =>
        DemandExecutable(workflowExecutionContext.WorkflowGraph);

    private static bool IsCompensationScope(IActivity activity)
    {
        if (activity is CompensationScope)
            return true;

        if (string.Equals(activity.Type, CompensationScope.ActivityTypeName, StringComparison.Ordinal))
            return true;

        return activity is NotFoundActivity notFound &&
               (string.Equals(notFound.MissingTypeName, CompensationScope.ActivityTypeName, StringComparison.Ordinal) ||
                IsCompensationScopeJson(notFound.OriginalActivityJson));
    }

    private static bool IsCompensationScopeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (root.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String &&
                string.Equals(type.GetString(), CompensationScope.ActivityTypeName, StringComparison.Ordinal))
                return true;

            if (root.TryGetProperty("originalActivityJson", out var original) && original.ValueKind == JsonValueKind.String)
                return IsCompensationScopeJson(original.GetString());
        }
        catch (JsonException)
        {
            // Malformed opaque activities retain their existing NotFound behavior.
        }

        return false;
    }
}
