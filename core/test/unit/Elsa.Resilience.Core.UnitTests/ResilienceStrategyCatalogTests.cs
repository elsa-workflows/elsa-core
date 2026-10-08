using Elsa.Resilience.Core.UnitTests.TestHelpers;
using NSubstitute;
using Open.Linq.AsyncExtensions;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategyCatalogTests
{
    private static ResilienceStrategyCatalog CreateCatalog(params IResilienceStrategySource[] sources) => new(sources);

    [Fact(DisplayName = "Catalog with no sources should return empty list")]
    public async Task ListAsync_NoProviders_ReturnsEmptyList()
    {
        var catalog = CreateCatalog();
        var result = await catalog.ListAsync();

        Assert.Empty(result);
    }

    [Fact(DisplayName = "Catalog should return all strategies from a single source")]
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

        Assert.Collection(result,
            s => Assert.Equal("strategy1", s.Id),
            s => Assert.Equal("strategy2", s.Id));
    }

    [Fact(DisplayName = "Catalog should combine strategies from multiple sources")]
    public async Task ListAsync_MultipleProviders_CombinesAllStrategies()
    {
        var provider1 = TestDataFactory.CreateStrategySource(
            TestDataFactory.CreateStrategy("strategy1", "Strategy 1"),
            TestDataFactory.CreateStrategy("strategy2", "Strategy 2"));
        var provider2 = TestDataFactory.CreateStrategySource(
            TestDataFactory.CreateStrategy("strategy3", "Strategy 3"));
        var catalog = CreateCatalog(provider1, provider2);

        var result = await catalog.ListAsync().ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, s => s.Id == "strategy1");
        Assert.Contains(result, s => s.Id == "strategy2");
        Assert.Contains(result, s => s.Id == "strategy3");
    }

    [Theory(DisplayName = "Catalog should retrieve strategy by ID or return null if not found")]
    [InlineData("test-id", "Test Strategy", true)]
    [InlineData("non-existent", null, false)]
    public async Task GetAsync_WithStrategyId_ReturnsExpectedResult(string searchId, string? expectedDisplayName, bool shouldExist)
    {
        var strategy = TestDataFactory.CreateStrategy("test-id", "Test Strategy");
        var provider = TestDataFactory.CreateStrategySource(strategy);
        var catalog = CreateCatalog(provider);

        var result = await catalog.GetAsync(searchId);

        if (shouldExist)
        {
            Assert.NotNull(result);
            Assert.Equal(searchId, result.Id);
            Assert.Equal(expectedDisplayName, result.DisplayName);
        }
        else
        {
            Assert.Null(result);
        }
    }

    [Fact(DisplayName = "Catalog should search all sources to find a strategy")]
    public async Task GetAsync_MultipleProvidersStrategyInSecond_ReturnsStrategy()
    {
        var provider1 = TestDataFactory.CreateStrategySource(TestDataFactory.CreateStrategy("strategy1", "Strategy 1"));
        var provider2 = TestDataFactory.CreateStrategySource(TestDataFactory.CreateStrategy("strategy2", "Strategy 2"));
        var catalog = CreateCatalog(provider1, provider2);

        var result = await catalog.GetAsync("strategy2");

        Assert.NotNull(result);
        Assert.Equal("strategy2", result.Id);
    }

    [Fact(DisplayName = "Catalog should cache strategy list after first retrieval")]
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

    [Fact(DisplayName = "GetAsync should use cached list when available")]
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
