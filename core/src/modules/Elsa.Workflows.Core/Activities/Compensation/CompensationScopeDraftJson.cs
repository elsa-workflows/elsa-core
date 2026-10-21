using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Activities.Compensation;

/// <summary>
/// Reads protected compensation pins before the normal activity converter can apply Latest fallback.
/// </summary>
internal static class CompensationScopeDraftJson
{
    public static bool IsRecognizedScope(JsonElement activity) =>
        activity.ValueKind == JsonValueKind.Object &&
        activity.TryGetProperty("type", out var type) &&
        type.ValueKind == JsonValueKind.String &&
        string.Equals(type.GetString(), CompensationScope.ActivityTypeName, StringComparison.Ordinal);

    public static bool IsMaterializable(JsonElement scope, IActivityRegistry activityRegistry)
    {
        if (!IsRecognizedScope(scope) ||
            !HasExactVersion(scope, CompensationScope.ActivityTypeName, 1, activityRegistry) ||
            !TryGetInt32(scope, "draftSchemaVersion", out var schemaVersion) ||
            schemaVersion != CompensationScope.CurrentDraftSchemaVersion ||
            !scope.TryGetProperty("steps", out var steps) ||
            steps.ValueKind != JsonValueKind.Array)
            return false;

        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object ||
                !step.TryGetProperty("stepId", out var stepId) ||
                stepId.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(stepId.GetString()) ||
                !stepIds.Add(stepId.GetString()!))
                return false;

            if (step.TryGetProperty("mappings", out var mappings) && mappings.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var role in new[] { "forward", "handler" })
            {
                if (!step.TryGetProperty(role, out var child) || child.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    continue;

                if (!IsPinnedActivityTree(child, activityRegistry))
                    return false;
            }
        }

        if (scope.TryGetProperty("onForwardFault", out var handoff) &&
            (handoff.ValueKind != JsonValueKind.String ||
             handoff.GetString() is not (CompensationFaultHandoff.Compensated or CompensationFaultHandoff.PropagateOriginalFault)))
            return false;

        return true;
    }

    private static bool IsPinnedActivityTree(JsonElement activity, IActivityRegistry activityRegistry)
    {
        if (activity.ValueKind != JsonValueKind.Object ||
            !activity.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            return false;

        var type = typeElement.GetString();
        if (string.IsNullOrWhiteSpace(type) ||
            string.Equals(type, ActivityTypeNameHelper.GenerateTypeName<NotFoundActivity>(), StringComparison.Ordinal) ||
            activity.TryGetProperty("workflowDefinitionId", out _) ||
            activity.TryGetProperty("workflowDefinitionVersionId", out _) ||
            !TryGetInt32(activity, "version", out var version) ||
            activityRegistry.Find(type, version) is not { } descriptor ||
            IsWorkflowDefinitionActivity(descriptor))
            return false;

        foreach (var childProperty in GetActivityChildProperties(descriptor))
        {
            if (!activity.TryGetProperty(childProperty.Name, out var childValue) || childValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                continue;

            if (childProperty.IsCollection)
            {
                if (childValue.ValueKind != JsonValueKind.Array ||
                    childValue.EnumerateArray().Any(child => !IsPinnedActivityTree(child, activityRegistry)))
                    return false;
            }
            else if (!IsPinnedActivityTree(childValue, activityRegistry))
                return false;
        }

        return true;
    }

    private static IEnumerable<(string Name, bool IsCollection)> GetActivityChildProperties(ActivityDescriptor descriptor)
    {
        foreach (var property in descriptor.ClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            var propertyType = property.PropertyType;
            var isCollection = false;
            if (!typeof(IActivity).IsAssignableFrom(propertyType))
            {
                var elementType = propertyType.IsArray
                    ? propertyType.GetElementType()
                    : propertyType.GetInterfaces()
                        .Append(propertyType)
                        .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        .Select(type => type.GetGenericArguments()[0])
                        .FirstOrDefault();
                if (elementType == null || !typeof(IActivity).IsAssignableFrom(elementType))
                    continue;
                isCollection = true;
            }

            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            yield return (jsonName, isCollection);
        }
    }

    private static bool HasExactVersion(JsonElement activity, string typeName, int expectedVersion, IActivityRegistry activityRegistry) =>
        TryGetInt32(activity, "version", out var version) &&
        version == expectedVersion &&
        activityRegistry.Find(typeName, version) != null;

    private static bool IsWorkflowDefinitionActivity(ActivityDescriptor descriptor) =>
        descriptor.CustomProperties.TryGetValue("RootType", out var rootType) &&
        string.Equals(rootType?.ToString(), "WorkflowDefinitionActivity", StringComparison.Ordinal);

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }
}
