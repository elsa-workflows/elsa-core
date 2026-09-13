using Elsa.Resilience.Core.UnitTests.TestHelpers;
using NSubstitute;
using Open.Linq.AsyncExtensions;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategyCatalogTests
{
    private static ResilienceStrategyCatalog CreateCatalog(params IResilienceStrategySource[] sources) => new(sources);

    [Test]
    [DisplayName("Catalog with no sources should return empty list")]
    public async Task ListAsync_NoProviders_ReturnsEmptyList()
    {
        var catalog = CreateCatalog();
        var result = await catalog.ListAsync();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    [DisplayName("Catalog should return all strategies from a single source")]
    public async Task ListAsync_SingleProviderWithStrategies_ReturnsStrategies()
    {
        var strategies = new[]
        {
            TestDataFactory.CreateStrategy("strategy1", "Strategy 1"),
            TestDataFactory.CreateStrategy("strategy2", "Strategy 2")
        };
        var provider = TestDataFactory.CreateStrategySource(strategies);
        var catalog = CreateCatalog(provider);

        var result = await catalog.ListAsync();
        await Assert.That(result.Select(x => x.Id)).IsEquivalentTo(
            ["strategy1", "strategy2"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Catalog should combine strategies from multiple sources")]
    public async Task ListAsync_MultipleProviders_CombinesAllStrategies()
    {
        var provider1 = TestDataFactory.CreateStrategySource(
            TestDataFactory.CreateStrategy("strategy1", "Strategy 1"),
            TestDataFactory.CreateStrategy("strategy2", "Strategy 2"));
        var provider2 = TestDataFactory.CreateStrategySource(
            TestDataFactory.CreateStrategy("strategy3", "Strategy 3"));
        var catalog = CreateCatalog(provider1, provider2);

        var result = await catalog.ListAsync().ToList();

        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result).Contains(s => s.Id == "strategy1");
        await Assert.That(result).Contains(s => s.Id == "strategy2");
        await Assert.That(result).Contains(s => s.Id == "strategy3");
    }

    [Test]
    [DisplayName("Catalog should retrieve strategy '$searchId' or return null if not found")]
    [Arguments("test-id", "Test Strategy", true)]
    [Arguments("non-existent", null, false)]
    public async Task GetAsync_WithStrategyId_ReturnsExpectedResult(string searchId, string? expectedDisplayName, bool shouldExist)
    {
        var strategy = TestDataFactory.CreateStrategy("test-id", "Test Strategy");
        var provider = TestDataFactory.CreateStrategySource(strategy);
        var catalog = CreateCatalog(provider);

        var result = await catalog.GetAsync(searchId);

        if (shouldExist)
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Id).IsEqualTo(searchId);
            await Assert.That(result.DisplayName).IsEqualTo(expectedDisplayName);
        }
        else
        {
            await Assert.That(result).IsNull();
        }
    }

    [Test]
    [DisplayName("Catalog should search all sources to find a strategy")]
    public async Task GetAsync_MultipleProvidersStrategyInSecond_ReturnsStrategy()
    {
        var provider1 = TestDataFactory.CreateStrategySource(TestDataFactory.CreateStrategy("strategy1", "Strategy 1"));
        var provider2 = TestDataFactory.CreateStrategySource(TestDataFactory.CreateStrategy("strategy2", "Strategy 2"));
        var catalog = CreateCatalog(provider1, provider2);

        var result = await catalog.GetAsync("strategy2");

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo("strategy2");
    }

    [Test]
    [DisplayName("Catalog should cache strategy list after first retrieval")]
    public async Task ListAsync_CalledMultipleTimes_CachesResult()
    {
        var strategy = TestDataFactory.CreateStrategy("test", "Test");
        var provider = TestDataFactory.CreateStrategySource(strategy);
        var catalog = CreateCatalog(provider);

        await catalog.ListAsync();
        await catalog.ListAsync();
        await catalog.ListAsync();

        await provider.Received(1).GetStrategiesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("GetAsync should use cached list when available")]
    public async Task GetAsync_CalledAfterList_UsesCachedResult()
    {
        var strategy = TestDataFactory.CreateStrategy("test", "Test");
        var provider = TestDataFactory.CreateStrategySource(strategy);
        var catalog = CreateCatalog(provider);

        await catalog.ListAsync();
        await catalog.GetAsync("test");

        await provider.Received(1).GetStrategiesAsync(Arg.Any<CancellationToken>());
    }
}
