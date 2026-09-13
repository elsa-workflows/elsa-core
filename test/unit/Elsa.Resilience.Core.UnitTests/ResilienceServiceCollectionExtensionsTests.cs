using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Extensions;
using Elsa.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services = new ServiceCollection();

    private ResilienceOptions GetOptions() => _services.BuildServiceProvider().GetRequiredService<IOptions<ResilienceOptions>>().Value;

    [Test]
    [DisplayName("AddResilienceStrategy should register the strategy type")]
    public async Task AddResilienceStrategy_RegistersType()
    {
        _services.AddResilienceStrategy<TestRetryStrategy>();

        await Assert.That(GetOptions().StrategyTypes).IsEquivalentTo(
            [typeof(TestRetryStrategy)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("AddResilienceStrategy should accumulate across calls")]
    public async Task AddResilienceStrategy_CalledTwice_RegistersBothTypes()
    {
        _services.AddResilienceStrategy<TestRetryStrategy>();
        _services.AddResilienceStrategy<TestNoopStrategy>();

        await Assert.That(GetOptions().StrategyTypes).IsEquivalentTo(
            [typeof(TestRetryStrategy), typeof(TestNoopStrategy)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("AddResilienceStrategies should register every supplied type")]
    public async Task AddResilienceStrategies_RegistersAllTypes()
    {
        _services.AddResilienceStrategies([typeof(TestRetryStrategy), typeof(TestNoopStrategy)]);

        await Assert.That(GetOptions().StrategyTypes).IsEquivalentTo(
            [typeof(TestRetryStrategy), typeof(TestNoopStrategy)],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("AddResilienceStrategies with an empty sequence should leave options untouched")]
    public async Task AddResilienceStrategies_EmptySequence_RegistersNothing()
    {
        _services.AddResilienceStrategies([]);

        await Assert.That(GetOptions().StrategyTypes).IsEmpty();
    }
}
