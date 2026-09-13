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
    private const string DefaultDefinitionId = "def-1";
    private const string DefaultVersionId = "version-id-1";
    private const string DefaultMaterializerName = "materializer";

    private readonly IWorkflowDefinitionService _decoratedService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowDefinitionCacheManager _cacheManager = Substitute.For<IWorkflowDefinitionCacheManager>();
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
    private readonly IMaterializerRegistry _materializerRegistry = Substitute.For<IMaterializerRegistry>();
    private readonly ICacheManager _cache = Substitute.For<ICacheManager>();

    public CachingWorkflowDefinitionServiceTests()
    {
        SetupCacheManager();
    }

    [Test]
    public async Task MaterializeWorkflowAsync_DelegatesToDecoratedService()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        _decoratedService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.MaterializeWorkflowAsync(definition);

        // Assert
        await Assert.That(result).IsSameReferenceAs(workflowGraph);
        await _decoratedService.Received(1).MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionIdAndVersionOptions_CreatesCacheKey()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        _cacheManager.CreateWorkflowDefinitionVersionCacheKey(DefaultDefinitionId, VersionOptions.Published).Returns("cache-key");
        _decoratedService.FindWorkflowDefinitionAsync(DefaultDefinitionId, VersionOptions.Published, Arg.Any<CancellationToken>()).Returns(definition);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(DefaultDefinitionId, VersionOptions.Published);

        // Assert
        await Assert.That(result).IsSameReferenceAs(definition);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey(DefaultDefinitionId, VersionOptions.Published);
    }

    [Test]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionVersionId_CreatesCacheKey()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        _cacheManager.CreateWorkflowDefinitionVersionCacheKey(DefaultVersionId).Returns("cache-key");
        _decoratedService.FindWorkflowDefinitionAsync(DefaultVersionId, Arg.Any<CancellationToken>()).Returns(definition);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(DefaultVersionId);

        // Assert
        await Assert.That(result).IsSameReferenceAs(definition);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey(DefaultVersionId);
    }

    [Test]
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
        await Assert.That(result).IsSameReferenceAs(definition);
        await _decoratedService.Received(1).FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Test]
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
        await Assert.That(result).IsSameReferenceAs(definition);
        _cacheManager.Received(1).CreateWorkflowDefinitionFilterCacheKey(filter);
    }

    [Test]
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
        await Assert.That(result).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("def-1", VersionOptions.Published);
    }

    [Test]
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
        await Assert.That(result).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("version-id-1");
    }

    [Test]
    public async Task FindWorkflowGraphAsync_ByHandle_ConvertsTFilterAndResolvesDefinitionAndGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        var handle = WorkflowDefinitionHandle.ByDefinitionId(DefaultDefinitionId, VersionOptions.Latest);

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(Arg.Any<WorkflowDefinitionFilter>()).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(handle);

        // Assert
        await Assert.That(result).IsSameReferenceAs(workflowGraph);
        await _decoratedService.Received(1).FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FindWorkflowGraphAsync_ByFilter_ResolvesDefinitionFirstAndCachesGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        var filter = new WorkflowDefinitionFilter { DefinitionId = DefaultDefinitionId };

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(filter);

        // Assert
        await Assert.That(result).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowDefinitionFilterCacheKey(filter);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey(DefaultVersionId);
    }

    [Test]
    public async Task FindWorkflowGraphAsync_ByFilter_WithUnavailableMaterializer_ReturnsNull()
    {
        // Arrange
        const string unavailableMaterializer = "unavailable-materializer";
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, unavailableMaterializer);
        definition.Id = DefaultVersionId;
        var filter = new WorkflowDefinitionFilter { DefinitionId = DefaultDefinitionId };

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter).Returns("cache-key");
        _decoratedService.FindWorkflowDefinitionAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        _materializerRegistry.IsMaterializerAvailable(unavailableMaterializer).Returns(false);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(filter);

        // Assert
        await Assert.That(result).IsNull();
        _cacheManager.Received(1).CreateWorkflowDefinitionFilterCacheKey(filter);
        await _decoratedService.DidNotReceive().MaterializeWorkflowAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FindWorkflowGraphsAsync_WithMultipleDefinitions_CachesEachGraph()
    {
        // Arrange
        var definition1 = TestHelpers.CreateWorkflowDefinition("def-1", DefaultMaterializerName);
        definition1.Id = "id-1";
        var definition2 = TestHelpers.CreateWorkflowDefinition("def-2", DefaultMaterializerName);
        definition2.Id = "id-2";
        var (_, graph1) = CreateWorkflowAndGraph("def-1");
        var (_, graph2) = CreateWorkflowAndGraph("def-2");
        var filter = new WorkflowDefinitionFilter();

        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(new[] { definition1, definition2 });
        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key-1");
        _cacheManager.CreateWorkflowVersionCacheKey("id-2").Returns("cache-key-2");
        SetupDefinitionAndGraphMaterialization(definition1, graph1);
        SetupDefinitionAndGraphMaterialization(definition2, graph2);
        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphsAsync(filter);

        // Assert
        var graphs = result.ToList();
        await Assert.That(graphs.Count).IsEqualTo(2);
        await Assert.That(graphs).Contains(graph1);
        await Assert.That(graphs).Contains(graph2);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("id-1");
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey("id-2");
    }

    [Test]
    public async Task FindWorkflowGraphsAsync_SkipsDefinitionsWithUnavailableMaterializer()
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
        _materializerRegistry.IsMaterializerAvailable("available-materializer").Returns(true);
        _materializerRegistry.IsMaterializerAvailable("unavailable-materializer").Returns(false);
        _decoratedService.MaterializeWorkflowAsync(definition1, Arg.Any<CancellationToken>()).Returns(graph1);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphsAsync(filter);

        // Assert
        var graphs = result.ToList();
        await Assert.That(graphs).HasSingleItem();
        await Assert.That(graphs[0]).IsSameReferenceAs(graph1);
        await _decoratedService.DidNotReceive().MaterializeWorkflowAsync(definition2, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_ResolvesDefinitionAndGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);

        _cacheManager.CreateWorkflowDefinitionVersionCacheKey(DefaultDefinitionId, VersionOptions.Published).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(DefaultDefinitionId, VersionOptions.Published, Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(DefaultDefinitionId, VersionOptions.Published);

        // Assert
        await Assert.That(result.WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(result.WorkflowGraph).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey(DefaultDefinitionId, VersionOptions.Published);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey(DefaultVersionId);
    }

    [Test]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionVersionId_ResolvesDefinitionAndGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);

        _cacheManager.CreateWorkflowDefinitionVersionCacheKey(DefaultVersionId).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(DefaultVersionId, Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(DefaultVersionId);

        // Assert
        await Assert.That(result.WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(result.WorkflowGraph).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowDefinitionVersionCacheKey(DefaultVersionId);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey(DefaultVersionId);
    }

    [Test]
    public async Task TryFindWorkflowGraphAsync_ByHandle_ConvertsTFilterAndResolvesDefinitionAndGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        var handle = WorkflowDefinitionHandle.ByDefinitionId(DefaultDefinitionId, VersionOptions.Latest);

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(Arg.Any<WorkflowDefinitionFilter>()).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(handle);

        // Assert
        await Assert.That(result.WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(result.WorkflowGraph).IsSameReferenceAs(workflowGraph);
        await _decoratedService.Received(1).FindWorkflowDefinitionAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TryFindWorkflowGraphAsync_ByFilter_ResolvesDefinitionAndGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = DefaultVersionId;
        var (_, workflowGraph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        var filter = new WorkflowDefinitionFilter { DefinitionId = DefaultDefinitionId };

        _cacheManager.CreateWorkflowDefinitionFilterCacheKey(filter).Returns("cache-key-def");
        _cacheManager.CreateWorkflowVersionCacheKey(DefaultVersionId).Returns("cache-key-graph");
        _decoratedService.FindWorkflowDefinitionAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        SetupDefinitionAndGraphMaterialization(definition, workflowGraph);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(filter);

        // Assert
        await Assert.That(result.WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(result.WorkflowGraph).IsSameReferenceAs(workflowGraph);
        _cacheManager.Received(1).CreateWorkflowDefinitionFilterCacheKey(filter);
        _cacheManager.Received(1).CreateWorkflowVersionCacheKey(DefaultVersionId);
    }

    [Test]
    public async Task TryFindWorkflowGraphsAsync_WithAvailableMaterializer_CachesGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, DefaultMaterializerName);
        definition.Id = "id-1";
        var (_, graph) = CreateWorkflowAndGraph(DefaultDefinitionId);
        var filter = new WorkflowDefinitionFilter();

        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(new[] { definition });
        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key");
        SetupDefinitionAndGraphMaterialization(definition, graph);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphsAsync(filter);

        // Assert
        var results = result.ToList();
        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(results[0].WorkflowGraph).IsSameReferenceAs(graph);
    }

    [Test]
    public async Task TryFindWorkflowGraphsAsync_WithUnavailableMaterializer_ReturnsNullGraph()
    {
        // Arrange
        const string unavailableMaterializer = "unavailable-materializer";
        var definition = TestHelpers.CreateWorkflowDefinition(DefaultDefinitionId, unavailableMaterializer);
        definition.Id = "id-1";
        var filter = new WorkflowDefinitionFilter();

        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(new[] { definition });
        _cacheManager.CreateWorkflowVersionCacheKey("id-1").Returns("cache-key");
        _materializerRegistry.IsMaterializerAvailable(unavailableMaterializer).Returns(false);
        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphsAsync(filter);

        // Assert
        var results = result.ToList();
        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].WorkflowDefinition).IsSameReferenceAs(definition);
        await Assert.That(results[0].WorkflowGraph).IsNull();
        await _decoratedService.DidNotReceive().MaterializeWorkflowAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Test]
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
        await Assert.That(results.Count).IsEqualTo(2);

        var result1 = results.First(r => r.WorkflowDefinition?.DefinitionId == "def-1");
        await Assert.That(result1.WorkflowDefinition).IsSameReferenceAs(definition1);
        await Assert.That(result1.WorkflowGraph).IsSameReferenceAs(graph1);

        var result2 = results.First(r => r.WorkflowDefinition?.DefinitionId == "def-2");
        await Assert.That(result2.WorkflowDefinition).IsSameReferenceAs(definition2);
        await Assert.That(result2.WorkflowGraph).IsNull();
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
            .Returns(async callInfo => (WorkflowDefinition?)await callInfo.Arg<Func<ICacheEntry, Task<WorkflowDefinition>>>()(Substitute.For<ICacheEntry>()));

        _cache.FindOrCreateAsync<WorkflowGraph>(Arg.Any<object>(), Arg.Any<Func<ICacheEntry, Task<WorkflowGraph>>>())
            .Returns(async callInfo => (WorkflowGraph?)await callInfo.Arg<Func<ICacheEntry, Task<WorkflowGraph>>>()(Substitute.For<ICacheEntry>()));
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
    /// Sets up a complete workflow definition with graph materialization for testing scenarios
    /// where definition needs to be resolved and materialized into a graph.
    /// </summary>
    private void SetupDefinitionAndGraphMaterialization(
        WorkflowDefinition definition,
        WorkflowGraph workflowGraph,
        string? materializerName = null)
    {
        materializerName ??= definition.MaterializerName;
        _materializerRegistry.IsMaterializerAvailable(materializerName).Returns(true);
        _decoratedService.MaterializeWorkflowAsync(definition, Arg.Any<CancellationToken>()).Returns(workflowGraph);
    }
}
