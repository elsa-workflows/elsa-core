using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        return HasPinnedOwnedActivityDescendants(activity, descriptor.ClrType, activityRegistry);
    }

    private static bool HasPinnedOwnedActivityDescendants(JsonElement value, Type containerType, IActivityRegistry activityRegistry)
    {
        foreach (var property in GetSerializableProperties(containerType))
        {
            if (!HasOwnedActivityDescendant(property.PropertyType, new HashSet<Type>()))
                continue;

            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!value.TryGetProperty(jsonName, out var childValue) || childValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                continue;

            if (!HasPinnedOwnedActivityValues(childValue, property.PropertyType, activityRegistry))
                return false;
        }

        return true;
    }

    private static bool HasPinnedOwnedActivityValues(JsonElement value, Type valueType, IActivityRegistry activityRegistry)
    {
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (typeof(IActivity).IsAssignableFrom(valueType))
            return IsPinnedActivityTree(value, activityRegistry);

        if (TryGetEnumerableElementType(valueType, out var elementType))
        {
            if (value.ValueKind != JsonValueKind.Array)
                return false;

            return value.EnumerateArray().All(item => item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
                                                    HasPinnedOwnedActivityValues(item, elementType, activityRegistry));
        }

        if (!HasOwnedActivityDescendant(valueType, new HashSet<Type>()))
            return true;

        return value.ValueKind == JsonValueKind.Object && HasPinnedOwnedActivityDescendants(value, valueType, activityRegistry);
    }

    private static bool HasOwnedActivityDescendant(Type type, HashSet<Type> visited)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (typeof(IActivity).IsAssignableFrom(type))
            return true;

        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(JsonElement) ||
            typeof(JsonDocument).IsAssignableFrom(type) || typeof(JsonNode).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type))
            return false;

        if (TryGetEnumerableElementType(type, out var elementType))
            return HasOwnedActivityDescendant(elementType, visited);

        if ((!type.IsClass && !type.IsValueType && !type.IsInterface) || !visited.Add(type))
            return false;

        var containsActivity = GetSerializableProperties(type)
            .Any(property => HasOwnedActivityDescendant(property.PropertyType, visited));
        visited.Remove(type);
        return containsActivity;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        elementType = null!;
        if (type == typeof(string))
            return false;

        var enumerableType = type.IsArray
            ? typeof(IEnumerable<>).MakeGenericType(type.GetElementType()!)
            : type.GetInterfaces().Append(type)
                .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableType == null)
            return false;

        elementType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is not { Condition: not JsonIgnoreCondition.Never });
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
