using Elsa.Caching;
using Elsa.Caching.Options;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class CachingWorkflowDefinitionServiceTests
{
    private readonly IWorkflowDefinitionService _decoratedService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowDefinitionCacheManager _cacheManager = Substitute.For<IWorkflowDefinitionCacheManager>();
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
    private readonly IMaterializerRegistry _materializerRegistry = Substitute.For<IMaterializerRegistry>();
    private readonly ICacheManager _cache = Substitute.For<ICacheManager>();

    public CachingWorkflowDefinitionServiceTests()
    {
        SetupCacheManager();
    }

    [Fact]
    public async Task MaterializeWorkflowAsync_DelegatesToDecoratedService()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var (_, workflowGraph) = CreateWorkflowAndGraph("def-1");

        _decoratedService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.MaterializeWorkflowAsync(definition);

        // Assert
        Assert.Same(workflowGraph, result);
        await _decoratedService.Received(1).MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionIdAndVersionOptions_CreatesCacheKey()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var cacheKey = "cache-key-1";

        _cacheManager.CreateWorkflowDefinitionVersionCacheKey("def-1", VersionOptions.Published).Returns(cacheKey);
        _decoratedService.FindWorkflowDefinitionAsync("def-1", VersionOptions.Published, Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Same(definition, result);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey("def-1", VersionOptions.Published);
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionVersionId_CreatesCacheKey()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition.Id = "version-id-1";
        var cacheKey = "cache-key-version-1";

        _cacheManager.CreateWorkflowDefinitionVersionCacheKey("version-id-1").Returns(cacheKey);
        _decoratedService.FindWorkflowDefinitionAsync("version-id-1", Arg.Any<CancellationToken>()).Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync("version-id-1");

        // Assert
        Assert.Same(definition, result);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey("version-id-1");
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByHandle_ConvertsTFilterAndDelegates()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(Arg.Any<WorkflowDefinitionFilter>()).Returns(cacheKey);
        _decoratedService.FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(handle);

        // Assert
        Assert.Same(definition, result);
        await _decoratedService.Received(1).FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByFilter_CreatesCacheKey()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter).Returns(cacheKey);
        _decoratedService.FindWorkflowDefinitionAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(filter);

        // Assert
        Assert.Same(definition, result);
        _cacheManager.Received(1).CreateWorkflowDefinitionFilterCacheKey(filter);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_CreatesCacheKey()
    {
        // Arrange
        var (_, workflowGraph) = CreateWorkflowAndGraph("def-1");
        var cacheKey = "cache-key-graph";

        _cacheManager.CreateWorkflowVersionCacheKey("def-1", VersionOptions.Published).Returns(cacheKey);
        _decoratedService.FindWorkflowGraphAsync("def-1", VersionOptions.Published, Arg.Any<CancellationToken>())
            .Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Same(workflowGraph, result);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("def-1", VersionOptions.Published);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionVersionId_CreatesCacheKey()
    {
        // Arrange
        var (_, workflowGraph) = CreateWorkflowAndGraph("def-1");
        var cacheKey = "cache-key-graph-version";

        _cacheManager.CreateWorkflowVersionCacheKey("version-id-1").Returns(cacheKey);
        _decoratedService.FindWorkflowGraphAsync("version-id-1", Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("version-id-1");

        // Assert
        Assert.Same(workflowGraph, result);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("version-id-1");
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByHandle_ConvertsTFilterAndDelegates()
    {
        // Arrange
        var (_, workflowGraph) = CreateWorkflowAndGraph("def-1");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowFilterCacheKey(Arg.Any<WorkflowDefinitionFilter>()).Returns(cacheKey);
        _decoratedService.FindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(handle);

        // Assert
        Assert.Same(workflowGraph, result);
        await _decoratedService.Received(1).FindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByFilter_CreatesCacheKey()
    {
        // Arrange
        var (_, workflowGraph) = CreateWorkflowAndGraph("def-1");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowFilterCacheKey(filter).Returns(cacheKey);
        _decoratedService.FindWorkflowGraphAsync(filter, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(filter);

        // Assert
        Assert.Same(workflowGraph, result);
        _cacheManager.Received(1).CreateWorkflowFilterCacheKey(filter);
    }

    [Fact]
    public async Task FindWorkflowGraphsAsync_WithMultipleDefinitions_CachesEachGraph()
    {
        // Arrange
        var definition1 = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition1.Id = "id-1";
        var definition2 = TestHelpers.CreateWorkflowDefinition("def-2", "materializer");
        definition2.Id = "id-2";
        var definitions = new[] { definition1, definition2 };

        var (_, graph1) = CreateWorkflowAndGraph("def-1");
        var (_, graph2) = CreateWorkflowAndGraph("def-2");

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key-1");
        _cacheManager.CreateWorkflowVersionCacheKey("id-2").Returns("cache-key-2");

        _decoratedService.MaterializeWorkflowAsync(definition1, Arg.Any<CancellationToken>()).Returns(graph1);
        _decoratedService.MaterializeWorkflowAsync(definition2, Arg.Any<CancellationToken>()).Returns(graph2);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphsAsync(filter);

        // Assert
        var graphs = result.ToList();
        Assert.Equal(2, graphs.Count);
        Assert.Contains(graph1, graphs);
        Assert.Contains(graph2, graphs);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("id-1");
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("id-2");
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_CreatesCacheKey()
    {
        // Arrange
        var findResult = CreateWorkflowGraphFindResult("def-1");
        var cacheKey = "cache-key-try";

        _cacheManager.CreateWorkflowVersionCacheKey("def-1", VersionOptions.Published).Returns(cacheKey);
        _decoratedService.TryFindWorkflowGraphAsync("def-1", VersionOptions.Published, Arg.Any<CancellationToken>())
            .Returns(findResult);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Same(findResult, result);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("def-1", VersionOptions.Published);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionVersionId_CreatesCacheKey()
    {
        // Arrange
        var findResult = CreateWorkflowGraphFindResult("def-1");
        var cacheKey = "cache-key-try-version";

        _cacheManager.CreateWorkflowVersionCacheKey("version-id-1").Returns(cacheKey);
        _decoratedService.TryFindWorkflowGraphAsync("version-id-1", Arg.Any<CancellationToken>()).Returns(findResult);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("version-id-1");

        // Assert
        Assert.Same(findResult, result);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("version-id-1");
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByHandle_ConvertsTFilterAndDelegates()
    {
        // Arrange
        var findResult = CreateWorkflowGraphFindResult("def-1");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowFilterCacheKey(Arg.Any<WorkflowDefinitionFilter>()).Returns(cacheKey);
        _decoratedService.TryFindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(findResult);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(handle);

        // Assert
        Assert.Same(findResult, result);
        await _decoratedService.Received(1).TryFindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByFilter_CreatesCacheKey()
    {
        // Arrange
        var findResult = CreateWorkflowGraphFindResult("def-1");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        var cacheKey = "cache-key-filter";

        _cacheManager.CreateWorkflowFilterCacheKey(filter).Returns(cacheKey);
        _decoratedService.TryFindWorkflowGraphAsync(filter, Arg.Any<CancellationToken>()).Returns(findResult);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(filter);

        // Assert
        Assert.Same(findResult, result);
        _cacheManager.Received(1).CreateWorkflowFilterCacheKey(filter);
    }

    [Fact]
    public async Task TryFindWorkflowGraphsAsync_WithAvailableMaterializer_CachesGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition.Id = "id-1";
        var definitions = new[] { definition };

        var (_, graph) = CreateWorkflowAndGraph("def-1");

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key-1");
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);
        _decoratedService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(graph);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphsAsync(filter);

        // Assert
        var results = result.ToList();
        Assert.Single(results);
        Assert.Same(definition, results[0].WorkflowDefinition);
        Assert.Same(graph, results[0].WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphsAsync_WithUnavailableMaterializer_ReturnsNullGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "unavailable-materializer");
        definition.Id = "id-1";
        var definitions = new[] { definition };

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key-1");
        _materializerRegistry.IsMaterializerAvailable("unavailable-materializer").Returns(false);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphsAsync(filter);

        // Assert
        var results = result.ToList();
        Assert.Single(results);
        Assert.Same(definition, results[0].WorkflowDefinition);
        Assert.Null(results[0].WorkflowGraph);
        await _decoratedService.DidNotReceive().MaterializeWorkflowAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFindWorkflowGraphsAsync_WithMixedMaterializers_HandlesEachCorrectly()
    {
        // Arrange
        var definition1 = TestHelpers.CreateWorkflowDefinition("def-1", "available-materializer");
        definition1.Id = "id-1";
        var definition2 = TestHelpers.CreateWorkflowDefinition("def-2", "unavailable-materializer");
        definition2.Id = "id-2";
        var definitions = new[] { definition1, definition2 };

        var (_, graph1) = CreateWorkflowAndGraph("def-1");

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key-1");
        _cacheManager.CreateWorkflowVersionCacheKey("id-2").Returns("cache-key-2");
        _materializerRegistry.IsMaterializerAvailable("available-materializer").Returns(true);
        _materializerRegistry.IsMaterializerAvailable("unavailable-materializer").Returns(false);
        _decoratedService.MaterializeWorkflowAsync(definition1, Arg.Any<CancellationToken>()).Returns(graph1);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphsAsync(filter);

        // Assert
        var results = result.ToList();
        Assert.Equal(2, results.Count);

        var result1 = results.First(r => r.WorkflowDefinition?.DefinitionId == "def-1");
        Assert.Same(definition1, result1.WorkflowDefinition);
        Assert.Same(graph1, result1.WorkflowGraph);

        var result2 = results.First(r => r.WorkflowDefinition?.DefinitionId == "def-2");
        Assert.Same(definition2, result2.WorkflowDefinition);
        Assert.Null(result2.WorkflowGraph);
    }

    private CachingWorkflowDefinitionService CreateService()
    {
        return new(
            _decoratedService,
            _cacheManager,
            _workflowDefinitionStore,
            _materializerRegistry);
    }

    /// <summary>
    /// Sets up the cache manager with proper mock behavior for all cache operations.
    /// </summary>
    private void SetupCacheManager()
    {
        _cacheManager.Cache.Returns(_cache);

        var cachingOptions = Microsoft.Extensions.Options.Options.Create(new CachingOptions { CacheDuration = TimeSpan.FromMinutes(10) });
        _cache.CachingOptions.Returns(cachingOptions);

        // Setup cache to call factory functions for all types
        _cache.GetOrCreateAsync<WorkflowDefinition>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowDefinition>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowDefinition>>>()(Substitute.For<ICacheEntry>()));

        _cache.GetOrCreateAsync<WorkflowGraph>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowGraph>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowGraph>>>()(Substitute.For<ICacheEntry>()));

        _cache.GetOrCreateAsync<WorkflowGraphFindResult>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowGraphFindResult>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowGraphFindResult>>>()(Substitute.For<ICacheEntry>()));

        _cache.FindOrCreateAsync<WorkflowDefinition>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowDefinition>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowDefinition>>>()(Substitute.For<ICacheEntry>()));

        _cache.FindOrCreateAsync<WorkflowGraph>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowGraph>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowGraph>>>()(Substitute.For<ICacheEntry>()));

        _cache.FindOrCreateAsync<WorkflowGraphFindResult>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowGraphFindResult>>>())
            .Returns(async callInfo => await callInfo.Arg<Func<ICacheEntry, Task<WorkflowGraphFindResult>>>()(Substitute.For<ICacheEntry>()));
    }

    /// <summary>
    /// Creates a workflow and workflow graph for testing.
    /// </summary>
    private (Workflow, WorkflowGraph) CreateWorkflowAndGraph(string definitionId = "def-1", int version = 1)
    {
        var workflow = new Workflow { Identity = new(definitionId, version, $"v{version}") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);
        return (workflow, workflowGraph);
    }

    /// <summary>
    /// Creates a workflow graph find result for testing.
    /// </summary>
    private WorkflowGraphFindResult CreateWorkflowGraphFindResult(string definitionId = "def-1", string materializerName = "materializer")
    {
        var definition = TestHelpers.CreateWorkflowDefinition(definitionId, materializerName);
        var (_, workflowGraph) = CreateWorkflowAndGraph(definitionId);
        return new(definition, workflowGraph);
    }
}
