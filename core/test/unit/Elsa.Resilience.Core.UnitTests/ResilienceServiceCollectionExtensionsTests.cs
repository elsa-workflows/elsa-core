using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Extensions;
using Elsa.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services = new ServiceCollection();

    private ResilienceOptions GetOptions() => _services.BuildServiceProvider().GetRequiredService<IOptions<ResilienceOptions>>().Value;

    [Fact(DisplayName = "AddResilienceStrategy should register the strategy type")]
    public void AddResilienceStrategy_RegistersType()
    {
        _services.AddResilienceStrategy<TestRetryStrategy>();

        Assert.Equal([typeof(TestRetryStrategy)], GetOptions().StrategyTypes);
    }

    [Fact(DisplayName = "AddResilienceStrategy should accumulate across calls")]
    public void AddResilienceStrategy_CalledTwice_RegistersBothTypes()
    {
        _services.AddResilienceStrategy<TestRetryStrategy>();
        _services.AddResilienceStrategy<TestNoopStrategy>();

        Assert.Equal([typeof(TestRetryStrategy), typeof(TestNoopStrategy)], GetOptions().StrategyTypes);
    }

    [Fact(DisplayName = "AddResilienceStrategies should register every supplied type")]
    public void AddResilienceStrategies_RegistersAllTypes()
    {
        _services.AddResilienceStrategies([typeof(TestRetryStrategy), typeof(TestNoopStrategy)]);

        Assert.Equal([typeof(TestRetryStrategy), typeof(TestNoopStrategy)], GetOptions().StrategyTypes);
    }

    [Fact(DisplayName = "AddResilienceStrategies with an empty sequence should leave options untouched")]
    public void AddResilienceStrategies_EmptySequence_RegistersNothing()
    {
        _services.AddResilienceStrategies([]);

        Assert.Empty(GetOptions().StrategyTypes);
    }
}
