using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Expressions.Models;
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

    public static bool IsMaterializable(JsonElement scope, IActivityRegistry activityRegistry, JsonSerializerOptions serializerOptions)
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

                // Legacy constructor cleanup interprets this metadata recursively. Keep an
                // ambiguous child payload in the whole opaque scope rather than rewrite it.
                if (ContainsTypeMetadata(child) || !IsPinnedActivityTree(child, activityRegistry, serializerOptions))
                    return false;
            }
        }

        if (scope.TryGetProperty("onForwardFault", out var handoff) &&
            (handoff.ValueKind != JsonValueKind.String ||
             handoff.GetString() is not (CompensationFaultHandoff.Compensated or CompensationFaultHandoff.PropagateOriginalFault)))
            return false;

        return true;
    }

    private static bool ContainsTypeMetadata(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (string.Equals(property.Name, "_type", StringComparison.Ordinal) || ContainsTypeMetadata(property.Value))
                    return true;
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Any(ContainsTypeMetadata);
        }

        return false;
    }

    private static bool IsPinnedActivityTree(JsonElement activity, IActivityRegistry activityRegistry, JsonSerializerOptions serializerOptions)
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

        return HasPinnedOwnedActivityDescendants(activity, descriptor.ClrType, activityRegistry, serializerOptions);
    }

    private static bool HasPinnedOwnedActivityDescendants(JsonElement value, Type containerType, IActivityRegistry activityRegistry, JsonSerializerOptions serializerOptions)
    {
        if (!TryGetSerializableProperties(containerType, serializerOptions, out var properties))
            return false;

        foreach (var property in properties)
        {
            if (!HasOwnedActivityDescendant(property.PropertyType, new HashSet<Type>(), serializerOptions))
                continue;

            if (!TryGetUniqueSerializableProperty(value, property.Name, serializerOptions.PropertyNameCaseInsensitive, out var childValue, out var hasChildValue))
                return false;

            if (!hasChildValue || childValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                continue;

            if (!HasPinnedOwnedActivityValues(childValue, property.PropertyType, activityRegistry, serializerOptions))
                return false;
        }

        return true;
    }

    private static bool TryGetUniqueSerializableProperty(JsonElement value, string propertyName, bool caseInsensitive, out JsonElement propertyValue, out bool found)
    {
        propertyValue = default;
        found = false;
        var comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var property in value.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, comparison))
                continue;

            if (found)
                return false;

            found = true;
            propertyValue = property.Value;
        }

        return true;
    }

    private static bool HasPinnedOwnedActivityValues(JsonElement value, Type valueType, IActivityRegistry activityRegistry, JsonSerializerOptions serializerOptions)
    {
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (typeof(IActivity).IsAssignableFrom(valueType))
            return IsPinnedActivityTree(value, activityRegistry, serializerOptions);

        if (TryGetEnumerableElementType(valueType, out var elementType))
        {
            if (value.ValueKind != JsonValueKind.Array)
                return false;

            return value.EnumerateArray().All(item => item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
                                                    HasPinnedOwnedActivityValues(item, elementType, activityRegistry, serializerOptions));
        }

        if (!HasOwnedActivityDescendant(valueType, new HashSet<Type>(), serializerOptions))
            return true;

        return value.ValueKind == JsonValueKind.Object && HasPinnedOwnedActivityDescendants(value, valueType, activityRegistry, serializerOptions);
    }

    private static bool HasOwnedActivityDescendant(Type type, HashSet<Type> visited, JsonSerializerOptions serializerOptions)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (typeof(IActivity).IsAssignableFrom(type))
            return true;

        if (type == typeof(string) || type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(JsonElement) ||
            type == typeof(object) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) ||
            type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri) || type == typeof(Type) ||
            typeof(JsonDocument).IsAssignableFrom(type) || typeof(JsonNode).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type) ||
            typeof(Input).IsAssignableFrom(type) || typeof(Expression).IsAssignableFrom(type))
            return false;

        if (TryGetEnumerableElementType(type, out var elementType))
            return HasOwnedActivityDescendant(elementType, visited, serializerOptions);

        if ((!type.IsClass && !type.IsValueType && !type.IsInterface) || !visited.Add(type))
            return false;

        if (!TryGetSerializableProperties(type, serializerOptions, out var properties))
        {
            visited.Remove(type);
            // A custom converter owns an opaque JSON shape. If this declared member is
            // present, the caller cannot prove that it contains no activity pins.
            return true;
        }

        var containsActivity = properties.Any(property => HasOwnedActivityDescendant(property.PropertyType, visited, serializerOptions));
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

    private static bool TryGetSerializableProperties(Type type, JsonSerializerOptions serializerOptions, out IReadOnlyList<JsonPropertyInfo> properties)
    {
        JsonTypeInfo typeInfo;
        try
        {
            typeInfo = serializerOptions.GetTypeInfo(type);
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            properties = [];
            return false;
        }

        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            properties = [];
            return false;
        }

        properties = typeInfo.Properties
            .Where(property => property.Get != null && !property.IsExtensionData)
            .Where(property => property.AttributeProvider is not MemberInfo member ||
                               member.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always)
            .ToArray();
        return true;
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
