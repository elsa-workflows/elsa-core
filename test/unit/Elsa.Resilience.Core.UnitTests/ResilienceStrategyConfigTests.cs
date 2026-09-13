using System.Text.Json.Nodes;
using Elsa.Expressions.Models;
using Elsa.Resilience.Models;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategyConfigTests
{
    [Test]
    [DisplayName("Config in identifier mode should round-trip through a JSON node")]
    public async Task SerializeToNode_IdentifierMode_RoundTrips()
    {
        var config = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Identifier,
            StrategyId = "my-strategy"
        };

        var result = ResilienceStrategyConfig.Deserialize(config.SerializeToNode());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Mode).IsEqualTo(ResilienceStrategyConfigMode.Identifier);
        await Assert.That(result.StrategyId).IsEqualTo("my-strategy");
        await Assert.That(result.Expression).IsNull();
    }

    [Test]
    [DisplayName("Config in expression mode should round-trip its expression through a JSON node")]
    public async Task SerializeToNode_ExpressionMode_RoundTripsExpression()
    {
        var config = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Expression,
            Expression = new("JavaScript", "getStrategy()")
        };

        var result = ResilienceStrategyConfig.Deserialize(config.SerializeToNode());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Mode).IsEqualTo(ResilienceStrategyConfigMode.Expression);
        await Assert.That(result.Expression).IsNotNull();
        await Assert.That(result.Expression.Type).IsEqualTo("JavaScript");
        await Assert.That(result.Expression.Value?.ToString()).IsEqualTo("getStrategy()");
    }

    [Test]
    [DisplayName("Deserializing a null node should return null")]
    public async Task Deserialize_NullNode_ReturnsNull()
    {
        await Assert.That(ResilienceStrategyConfig.Deserialize(null)).IsNull();
    }

    [Test]
    [DisplayName("Deserializing an unknown mode should fail rather than silently defaulting")]
    public async Task Deserialize_UnknownMode_Throws()
    {
        var node = JsonNode.Parse("""{"mode":"NotAMode"}""");

        await Assert.That(() => ResilienceStrategyConfig.Deserialize(node)).Throws<Exception>();
    }
}
