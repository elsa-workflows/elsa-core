using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class BooleanConditionAdapterTests
{
    private static readonly ISet<string> KnownProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "JavaScript"
    };

    [Fact]
    public void Inspect_ClassifiesMissingConditionAsMissing()
    {
        var result = BooleanConditionAdapter.Inspect(null, KnownProviders);

        Assert.Equal(BooleanConditionKind.Missing, result.Kind);
        Assert.Null(result.LiteralValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Inspect_ClassifiesScalarBoolean(bool value)
    {
        var result = BooleanConditionAdapter.Inspect(JsonValue.Create(value), KnownProviders);

        Assert.Equal(BooleanConditionKind.Literal, result.Kind);
        Assert.Equal(value, result.LiteralValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Inspect_ClassifiesWrappedLiteralBoolean(bool value)
    {
        var input = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject
            {
                ["type"] = "Literal",
                ["value"] = value
            }
        };

        var result = BooleanConditionAdapter.Inspect(input, KnownProviders);

        Assert.Equal(BooleanConditionKind.Literal, result.Kind);
        Assert.Equal(value, result.LiteralValue);
    }

    [Fact]
    public void Inspect_ClassifiesKnownProviderExpression()
    {
        var input = JsonNode.Parse("""
            {
              "typeName": "Boolean",
              "expression": { "type": "JavaScript", "value": "order.Total > 0" }
            }
            """)!;

        var result = BooleanConditionAdapter.Inspect(input, KnownProviders);

        Assert.Equal(BooleanConditionKind.Expression, result.Kind);
        Assert.Equal("JavaScript", result.ExpressionType);
        Assert.Equal("order.Total > 0", result.ExpressionValue);
    }

    [Fact]
    public void Inspect_ClassifiesUnavailableProviderAsUnknown()
    {
        var input = JsonNode.Parse("""
            {
              "typeName": "Boolean",
              "expression": { "type": "Liquid", "value": "order.total > 0" }
            }
            """)!;

        var result = BooleanConditionAdapter.Inspect(input, KnownProviders);

        Assert.Equal(BooleanConditionKind.Unknown, result.Kind);
        Assert.Equal("Liquid", result.ExpressionType);
        Assert.Equal("order.total > 0", result.ExpressionValue);
    }

    [Theory]
    [InlineData("{\"typeName\":\"Boolean\"}")]
    [InlineData("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"Literal\"}}")]
    [InlineData("{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"Literal\",\"value\":1}}")]
    [InlineData("{\"typeName\":\"String\",\"expression\":{\"type\":\"JavaScript\",\"value\":\"true\"}}")]
    [InlineData("[true]")]
    [InlineData("\"true\"")]
    public void Inspect_ClassifiesMalformedCondition(string source)
    {
        var result = BooleanConditionAdapter.Inspect(JsonNode.Parse(source), KnownProviders);

        Assert.Equal(BooleanConditionKind.Malformed, result.Kind);
    }

    [Fact]
    public void Inspect_ClassifiesInvalidJsonSlotMarkerAsMalformed()
    {
        var marker = new JsonObject
        {
            [StateMachineDesignerConstants.InvalidJsonSlotProperty] = StateMachineDesignerConstants.InvalidJsonSlotMarkerValue,
            [StateMachineDesignerConstants.InvalidJsonSlotSourceProperty] = "{ broken"
        };

        var result = BooleanConditionAdapter.Inspect(marker, KnownProviders);

        Assert.Equal(BooleanConditionKind.Malformed, result.Kind);
    }

    [Fact]
    public void SetLiteral_EmitsCanonicalBooleanInputWithoutMutatingSource()
    {
        var source = JsonNode.Parse("""
            {
              "typeName": "Boolean",
              "expression": { "type": "JavaScript", "value": "order.Total > 0" },
              "custom": { "keep": true }
            }
            """)!;

        var result = BooleanConditionAdapter.SetLiteral(source, true);

        Assert.NotSame(source, result);
        Assert.Equal("Boolean", result!["typeName"]!.GetValue<string>());
        Assert.Equal("Literal", result["expression"]!["type"]!.GetValue<string>());
        Assert.True(result["expression"]!["value"]!.GetValue<bool>());
        Assert.True(result["custom"]!["keep"]!.GetValue<bool>());
        Assert.Equal("JavaScript", source["expression"]!["type"]!.GetValue<string>());
        Assert.NotNull(source["custom"]);
    }

    [Fact]
    public void SetLiteral_PreservesOriginalReferenceWhenValueIsAlreadyTheSame()
    {
        var source = JsonNode.Parse("""
            {
              "typeName": "Boolean",
              "expression": { "type": "Literal", "value": true },
              "custom": "preserve"
            }
            """)!;

        var result = BooleanConditionAdapter.SetLiteral(source, true);

        Assert.Same(source, result);
        Assert.Equal("preserve", result["custom"]!.GetValue<string>());
    }

    [Fact]
    public void SetExpression_EmitsCanonicalBooleanInputAndPreservesNoOpReference()
    {
        var source = JsonNode.Parse("""
            {
              "typeName": "Boolean",
              "expression": { "type": "JavaScript", "value": "order.Total > 0" },
              "custom": "preserve"
            }
            """)!;

        var result = BooleanConditionAdapter.SetExpression(source, "JavaScript", "order.Total > 0");

        Assert.Same(source, result);

        var changed = BooleanConditionAdapter.SetExpression(source, "JavaScript", "order.Total >= 0");

        Assert.NotSame(source, changed);
        Assert.Equal("Boolean", changed!["typeName"]!.GetValue<string>());
        Assert.Equal("JavaScript", changed["expression"]!["type"]!.GetValue<string>());
        Assert.Equal("order.Total >= 0", changed["expression"]!["value"]!.GetValue<string>());
        Assert.Equal("preserve", changed["custom"]!.GetValue<string>());
        Assert.Equal("order.Total > 0", source["expression"]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void ExplicitEdit_RemovesShorthandSemanticPropertiesButPreservesMetadata()
    {
        var source = JsonNode.Parse("""
            {
              "type": "JavaScript",
              "value": "order.Total > 0",
              "custom": "preserve"
            }
            """)!;

        var result = BooleanConditionAdapter.SetLiteral(source, false);

        Assert.Null(result!["type"]);
        Assert.Null(result["value"]);
        Assert.Null(result["literal"]);
        Assert.Equal("preserve", result["custom"]!.GetValue<string>());
        Assert.Equal("Boolean", result["typeName"]!.GetValue<string>());
        Assert.False(result["expression"]!["value"]!.GetValue<bool>());
    }

    [Fact]
    public void TrySetAdvanced_PreservesOriginalReferenceForEquivalentJson()
    {
        var source = JsonNode.Parse("{ \"typeName\": \"Boolean\", \"expression\": { \"type\": \"Literal\", \"value\": true } }")!;

        var result = BooleanConditionAdapter.TrySetAdvanced(source, "{\"expression\":{\"value\":true,\"type\":\"Literal\"},\"typeName\":\"Boolean\"}");

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Same(source, result.Value);
    }

    [Fact]
    public void TrySetAdvanced_ReturnsOriginalAndErrorForInvalidJson()
    {
        var source = JsonNode.Parse("{ \"typeName\": \"Boolean\", \"expression\": { \"type\": \"Liquid\", \"value\": \"keep-me\" } }")!;

        var result = BooleanConditionAdapter.TrySetAdvanced(source, "{ invalid");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Same(source, result.Value);
        Assert.Equal("Liquid", source["expression"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void TrySetAdvanced_ReturnsOriginalAndErrorForValidMalformedCondition()
    {
        var source = JsonNode.Parse("{ \"typeName\": \"Boolean\", \"expression\": { \"type\": \"Liquid\", \"value\": \"keep-me\" } }")!;

        var result = BooleanConditionAdapter.TrySetAdvanced(source, "{\"typeName\":\"Boolean\",\"expression\":{\"type\":\"Literal\",\"value\":1}}");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Same(source, result.Value);
    }
}
