using Elsa.Resilience;
using NSubstitute;

namespace Elsa.Resilience.Core.UnitTests.TestHelpers;

internal static class TestDataFactory
{
    public static IResilienceStrategy CreateStrategy(string id, string displayName)
    {
        var strategy = Substitute.For<IResilienceStrategy>();
        strategy.Id.Returns(id);
        strategy.DisplayName.Returns(displayName);
        return strategy;
    }

    public static IResilienceStrategySource CreateStrategySource(params IResilienceStrategy[] strategies)
    {
        var source = Substitute.For<IResilienceStrategySource>();
        source.GetStrategiesAsync(Arg.Any<CancellationToken>()).Returns(strategies);
        return source;
    }
}
