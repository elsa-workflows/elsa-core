using System.Text.Json.Nodes;
using Elsa.Expressions.Models;
using Elsa.Resilience.Models;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategyConfigTests
{
    [Fact(DisplayName = "Config in identifier mode should round-trip through a JSON node")]
    public void SerializeToNode_IdentifierMode_RoundTrips()
    {
        var config = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Identifier,
            StrategyId = "my-strategy"
        };

        var result = ResilienceStrategyConfig.Deserialize(config.SerializeToNode());

        Assert.NotNull(result);
        Assert.Equal(ResilienceStrategyConfigMode.Identifier, result.Mode);
        Assert.Equal("my-strategy", result.StrategyId);
        Assert.Null(result.Expression);
    }

    [Fact(DisplayName = "Config in expression mode should round-trip its expression through a JSON node")]
    public void SerializeToNode_ExpressionMode_RoundTripsExpression()
    {
        var config = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Expression,
            Expression = new("JavaScript", "getStrategy()")
        };

        var result = ResilienceStrategyConfig.Deserialize(config.SerializeToNode());

        Assert.NotNull(result);
        Assert.Equal(ResilienceStrategyConfigMode.Expression, result.Mode);
        Assert.NotNull(result.Expression);
        Assert.Equal("JavaScript", result.Expression.Type);
        Assert.Equal("getStrategy()", result.Expression.Value?.ToString());
    }

    [Fact(DisplayName = "Deserializing a null node should return null")]
    public void Deserialize_NullNode_ReturnsNull()
    {
        Assert.Null(ResilienceStrategyConfig.Deserialize(null));
    }

    [Fact(DisplayName = "Deserializing an unknown mode should fail rather than silently defaulting")]
    public void Deserialize_UnknownMode_Throws()
    {
        var node = JsonNode.Parse("""{"mode":"NotAMode"}""");

        Assert.ThrowsAny<Exception>(() => ResilienceStrategyConfig.Deserialize(node));
    }
}
