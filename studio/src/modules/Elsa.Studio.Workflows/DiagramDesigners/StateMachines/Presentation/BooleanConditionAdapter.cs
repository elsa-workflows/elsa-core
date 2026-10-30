using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

/// <summary>
/// Describes the lossless forms a StateMachine transition condition can have in JSON.
/// </summary>
public enum BooleanConditionKind
{
    Missing,
    Literal,
    Expression,
    Unknown,
    Malformed
}

/// <summary>
/// A read-only projection of a condition JSON value.
/// </summary>
public sealed record BooleanConditionDescription(
    BooleanConditionKind Kind,
    bool? LiteralValue = null,
    string? ExpressionType = null,
    string? ExpressionValue = null);

/// <summary>
/// The result of applying an advanced JSON edit to a condition.
/// </summary>
public sealed record BooleanConditionEditResult(JsonNode? Value, string? Error)
{
    /// <summary>
    /// Gets a value indicating whether the edit produced a valid value.
    /// </summary>
    public bool Succeeded => Error == null;
}

/// <summary>
/// Reads and edits StateMachine boolean conditions without changing unknown JSON.
/// </summary>
public static class BooleanConditionAdapter
{
    /// <summary>
    /// The canonical backend type name for an Elsa boolean input.
    /// </summary>
    public const string BooleanTypeName = "Boolean";

    /// <summary>
    /// Inspects a condition. When provider names are supplied, an expression provider
    /// outside that set is classified as unknown and remains available through its source JSON.
    /// </summary>
    public static BooleanConditionDescription Inspect(JsonNode? value, IEnumerable<string>? knownExpressionTypes = null)
    {
        if (value == null)
            return new(BooleanConditionKind.Missing);

        if (value is JsonValue scalar && scalar.TryGetValue<bool>(out var scalarValue))
            return new(BooleanConditionKind.Literal, LiteralValue: scalarValue);

        if (value is not JsonObject obj)
            return new(BooleanConditionKind.Malformed);

        if (StateMachineDesignerConstants.IsInvalidJsonSlotMarker(obj))
            return new(BooleanConditionKind.Malformed);

        var knownTypes = knownExpressionTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Runtime Input<T> JSON uses { typeName, expression }. Keep support for the
        // older shorthand { type, value/expression } as it is still encountered in
        // imported definitions.
        if (obj["expression"] is JsonObject expressionObject)
            return InspectWrapped(obj, expressionObject, knownTypes);

        if (obj.ContainsKey("expression") || obj.ContainsKey("typeName"))
            return new(BooleanConditionKind.Malformed);

        return InspectShorthand(obj, knownTypes);
    }

    /// <summary>
    /// Sets a literal condition. A new canonical Boolean Input JSON value is emitted
    /// only when the requested value differs from the existing literal.
    /// </summary>
    public static JsonNode SetLiteral(JsonNode? original, bool value)
    {
        var description = Inspect(original);
        if (description.Kind == BooleanConditionKind.Literal && description.LiteralValue == value)
            return original!;

        return CreateCanonicalLiteral(original, value);
    }

    /// <summary>
    /// Sets a provider expression using canonical Boolean Input JSON.
    /// </summary>
    public static JsonNode SetExpression(JsonNode? original, string expressionType, string expressionValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expressionType);
        ArgumentNullException.ThrowIfNull(expressionValue);

        var description = Inspect(original);
        if ((description.Kind is BooleanConditionKind.Expression or BooleanConditionKind.Unknown)
            && string.Equals(description.ExpressionType, expressionType, StringComparison.Ordinal)
            && string.Equals(description.ExpressionValue, expressionValue, StringComparison.Ordinal))
        {
            return original!;
        }

        return CreateCanonicalExpression(original, expressionType, expressionValue);
    }

    /// <summary>
    /// Applies an advanced JSON edit. Invalid JSON returns the original node and an error;
    /// equivalent JSON returns the original node to preserve its identity and metadata.
    /// </summary>
    public static BooleanConditionEditResult TrySetAdvanced(JsonNode? original, string? source)
    {
        if (source == null)
            return new(original, "Condition JSON is required.");

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(source);
        }
        catch (JsonException)
        {
            return new(original, "Condition JSON is invalid.");
        }

        if (Inspect(parsed).Kind == BooleanConditionKind.Malformed)
            return new(original, "Condition JSON is not a valid Boolean condition.");

        return JsonNode.DeepEquals(original, parsed)
            ? new(original, null)
            : new(parsed, null);
    }

    private static BooleanConditionDescription InspectWrapped(
        JsonObject wrapper,
        JsonObject expression,
        ISet<string>? knownTypes)
    {
        var typeName = GetString(wrapper["typeName"]);
        if (wrapper.ContainsKey("typeName") && typeName == null)
            return new(BooleanConditionKind.Malformed);

        if (!string.IsNullOrWhiteSpace(typeName) && !IsBooleanInputType(typeName))
            return new(BooleanConditionKind.Malformed);

        var expressionType = GetString(expression["type"]);
        if (string.IsNullOrWhiteSpace(expressionType))
            return new(BooleanConditionKind.Malformed);

        if (string.Equals(expressionType, "Literal", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsBooleanInputType(typeName) && !string.IsNullOrWhiteSpace(typeName))
                return new(BooleanConditionKind.Malformed);

            if (TryGetBoolean(expression["value"], out var literalValue)
                || TryGetBoolean(expression["literal"], out literalValue))
            {
                return new(BooleanConditionKind.Literal, LiteralValue: literalValue);
            }

            return new(BooleanConditionKind.Malformed);
        }

        if (!HasValue(expression, "value") && !HasValue(expression, "expression"))
            return new(BooleanConditionKind.Malformed);

        var expressionValue = GetExpressionValue(expression);
        var kind = knownTypes == null || knownTypes.Contains(expressionType)
            ? BooleanConditionKind.Expression
            : BooleanConditionKind.Unknown;

        return new(kind, ExpressionType: expressionType, ExpressionValue: expressionValue);
    }

    private static BooleanConditionDescription InspectShorthand(JsonObject value, ISet<string>? knownTypes)
    {
        var type = GetString(value["type"]);
        if (string.IsNullOrWhiteSpace(type))
            return new(BooleanConditionKind.Unknown);

        if (string.Equals(type, "Literal", StringComparison.OrdinalIgnoreCase)
            || IsBooleanInputType(type))
        {
            if (TryGetBoolean(value["value"], out var literalValue)
                || TryGetBoolean(value["literal"], out literalValue))
            {
                return new(BooleanConditionKind.Literal, LiteralValue: literalValue);
            }

            return new(BooleanConditionKind.Malformed);
        }

        if (!HasValue(value, "expression") && !HasValue(value, "value"))
            return new(BooleanConditionKind.Unknown);

        var expressionValue = GetExpressionValue(value);
        var kind = knownTypes == null || knownTypes.Contains(type)
            ? BooleanConditionKind.Expression
            : BooleanConditionKind.Unknown;

        return new(kind, ExpressionType: type, ExpressionValue: expressionValue);
    }

    private static JsonObject CreateCanonicalLiteral(JsonNode? original, bool value)
    {
        var result = CloneForExplicitEdit(original);
        result["typeName"] = BooleanTypeName;
        result["expression"] = new JsonObject
        {
            ["type"] = "Literal",
            ["value"] = value
        };
        return result;
    }

    private static JsonObject CreateCanonicalExpression(JsonNode? original, string expressionType, string expressionValue)
    {
        var result = CloneForExplicitEdit(original);
        result["typeName"] = BooleanTypeName;
        result["expression"] = new JsonObject
        {
            ["type"] = expressionType,
            ["value"] = expressionValue
        };
        return result;
    }

    private static JsonObject CloneForExplicitEdit(JsonNode? original)
    {
        var result = original is JsonObject obj ? obj.DeepClone().AsObject() : [];

        // These properties describe the previous condition representation. They must
        // not survive beside the canonical wrapper, while all unrelated metadata does.
        result.Remove("type");
        result.Remove("value");
        result.Remove("literal");
        result.Remove("expression");
        result.Remove(StateMachineDesignerConstants.InvalidJsonSlotProperty);
        result.Remove(StateMachineDesignerConstants.InvalidJsonSlotSourceProperty);
        result.Remove("slot");
        return result;
    }

    private static bool HasValue(JsonObject obj, string propertyName) => obj.TryGetPropertyValue(propertyName, out var value) && value != null;

    private static string? GetString(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string? GetExpressionValue(JsonObject obj)
    {
        var value = obj.TryGetPropertyValue("value", out var valueNode) && valueNode != null
            ? valueNode
            : obj.TryGetPropertyValue("expression", out var expressionNode) && expressionNode != null
                ? expressionNode
                : null;

        return value switch
        {
            null => null,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
            _ => value.ToJsonString()
        };
    }

    private static bool IsBooleanInputType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        var normalized = typeName.Split(',', 2)[0].Trim();
        return normalized.Equals(BooleanTypeName, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("System.Boolean", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("bool", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Elsa.Boolean", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetBoolean(JsonNode? node, out bool value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value))
            return true;

        if (node is JsonValue stringValue
            && stringValue.TryGetValue<string>(out var text)
            && bool.TryParse(text, out value))
        {
            return true;
        }

        value = false;
        return false;
    }
}
