using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowDefinitionServiceTests
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
    private readonly IWorkflowGraphBuilder _workflowGraphBuilder = Substitute.For<IWorkflowGraphBuilder>();
    private readonly IMaterializerRegistry _materializerRegistry = Substitute.For<IMaterializerRegistry>();
    private readonly ILogger<WorkflowDefinitionService> _logger = Substitute.For<ILogger<WorkflowDefinitionService>>();

    [Fact]
    public async Task MaterializeWorkflowAsync_WithValidMaterializer_ReturnsMaterializedGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "test-materializer");
        var (_, workflowGraph) = SetupMaterializerAndGraphBuilder(definition);

        var service = CreateService();

        // Act
        var result = await service.MaterializeWorkflowAsync(definition);

        // Assert
        Assert.Same(workflowGraph, result);
    }

    [Fact]
    public async Task MaterializeWorkflowAsync_WithMissingMaterializer_ThrowsException()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "missing-materializer");
        _materializerRegistry.GetMaterializer("missing-materializer").Returns((IWorkflowMaterializer?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowMaterializerNotFoundException>(() => service.MaterializeWorkflowAsync(definition));
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionIdAndVersionOptions_ReturnsDefinition()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Same(definition, result);
        await _workflowDefinitionStore.Received(1).FindAsync(
            Arg.Any<WorkflowDefinitionFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByDefinitionVersionId_ReturnsDefinition()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition.Id = "version-id-1";
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync("version-id-1");

        // Assert
        Assert.Same(definition, result);
        await _workflowDefinitionStore.Received(1).FindAsync(
            Arg.Is<WorkflowDefinitionFilter>(f => f.Id == "version-id-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByHandle_ReturnsDefinition()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(handle);

        // Assert
        Assert.Same(definition, result);
        await _workflowDefinitionStore.Received(1).FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowDefinitionAsync_ByFilter_ReturnsDefinition()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        _workflowDefinitionStore.FindAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowDefinitionAsync(filter);

        // Assert
        Assert.Same(definition, result);
        await _workflowDefinitionStore.Received(1).FindAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WithAvailableMaterializer_ReturnsGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var workflowGraph = SetupCompleteWorkflowGraphMocking(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Same(workflowGraph, result);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WithUnavailableMaterializer_ReturnsNull()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        SetupCompleteWorkflowGraphMocking(definition, materializerAvailable: false);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WhenDefinitionNotFound_ReturnsNull()
    {
        // Arrange
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowDefinition?)null);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByDefinitionVersionId_ReturnsGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition.Id = "version-id-1";
        var workflowGraph = SetupCompleteWorkflowGraphMocking(definition);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync("version-id-1");

        // Assert
        Assert.Same(workflowGraph, result);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByHandle_ReturnsGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        var workflow = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(handle);

        // Assert
        Assert.Same(workflowGraph, result);
    }

    [Fact]
    public async Task FindWorkflowGraphAsync_ByFilter_ReturnsGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        var workflow = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        _workflowDefinitionStore.FindAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphAsync(filter);

        // Assert
        Assert.Same(workflowGraph, result);
    }

    [Fact]
    public async Task FindWorkflowGraphsAsync_WithMultipleDefinitions_ReturnsAllGraphs()
    {
        // Arrange
        var definition1 = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var definition2 = TestHelpers.CreateWorkflowDefinition("def-2", "materializer");
        var definitions = new[] { definition1, definition2 };

        var workflow1 = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflow2 = new Workflow { Identity = new("def-2", 1, "v1") };
        var graph1 = TestHelpers.CreateWorkflowGraph(workflow1);
        var graph2 = TestHelpers.CreateWorkflowGraph(workflow2);

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition1, Arg.Any<CancellationToken>()).Returns(workflow1);
        materializer.MaterializeAsync(definition2, Arg.Any<CancellationToken>()).Returns(workflow2);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);

        _workflowGraphBuilder.BuildAsync(workflow1, Arg.Any<CancellationToken>()).Returns(graph1);
        _workflowGraphBuilder.BuildAsync(workflow2, Arg.Any<CancellationToken>()).Returns(graph2);

        var service = CreateService();

        // Act
        var result = await service.FindWorkflowGraphsAsync(filter);

        // Assert
        var graphs = result.ToList();
        Assert.Equal(2, graphs.Count);
        Assert.Contains(graph1, graphs);
        Assert.Contains(graph2, graphs);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WithAvailableMaterializer_ReturnsSuccessResult()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var workflow = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.NotNull(result);
        Assert.Same(definition, result.WorkflowDefinition);
        Assert.Same(workflowGraph, result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WithUnavailableMaterializer_ReturnsResultWithNullGraph()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        SetupCompleteWorkflowGraphMocking(definition, materializerAvailable: false);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.NotNull(result);
        Assert.Same(definition, result.WorkflowDefinition);
        Assert.Null(result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionIdAndVersionOptions_WhenDefinitionNotFound_ReturnsEmptyResult()
    {
        // Arrange
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns((WorkflowDefinition?)null);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("def-1", VersionOptions.Published);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.WorkflowDefinition);
        Assert.Null(result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByDefinitionVersionId_ReturnsResult()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        definition.Id = "version-id-1";
        var workflowGraph = SetupCompleteWorkflowGraphMocking(definition);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync("version-id-1");

        // Assert
        Assert.NotNull(result);
        Assert.Same(definition, result.WorkflowDefinition);
        Assert.Same(workflowGraph, result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByHandle_ReturnsResult()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var handle = WorkflowDefinitionHandle.ByDefinitionId("def-1", VersionOptions.Latest);
        var workflow = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definition);
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(handle);

        // Assert
        Assert.NotNull(result);
        Assert.Same(definition, result.WorkflowDefinition);
        Assert.Same(workflowGraph, result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphAsync_ByFilter_ReturnsResult()
    {
        // Arrange
        var definition = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var filter = new WorkflowDefinitionFilter { DefinitionId = "def-1" };
        var workflow = new Workflow { Identity = new("def-1", 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        _workflowDefinitionStore.FindAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        var service = CreateService();

        // Act
        var result = await service.TryFindWorkflowGraphAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Same(definition, result.WorkflowDefinition);
        Assert.Same(workflowGraph, result.WorkflowGraph);
    }

    [Fact]
    public async Task TryFindWorkflowGraphsAsync_WithMultipleDefinitions_ReturnsAllResults()
    {
        // Arrange
        var definition1 = TestHelpers.CreateWorkflowDefinition("def-1", "materializer");
        var definition2 = TestHelpers.CreateWorkflowDefinition("def-2", "unavailable-materializer");
        var definitions = new[] { definition1, definition2 };

        var workflow1 = new Workflow { Identity = new("def-1", 1, "v1") };
        var graph1 = TestHelpers.CreateWorkflowGraph(workflow1);

        var filter = new WorkflowDefinitionFilter();
        _workflowDefinitionStore.FindManyAsync(filter, Arg.Any<CancellationToken>()).Returns(definitions);

        _materializerRegistry.IsMaterializerAvailable("materializer").Returns(true);
        _materializerRegistry.IsMaterializerAvailable("unavailable-materializer").Returns(false);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition1, Arg.Any<CancellationToken>()).Returns(workflow1);
        _materializerRegistry.GetMaterializer("materializer").Returns(materializer);

        _workflowGraphBuilder.BuildAsync(workflow1, Arg.Any<CancellationToken>()).Returns(graph1);

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

    private WorkflowDefinitionService CreateService()
    {
        return new(
            _workflowDefinitionStore,
            _workflowGraphBuilder,
            _materializerRegistry,
            _logger);
    }

    /// <summary>
    /// Sets up a materializer and graph builder for the given definition.
    /// Returns the workflow and workflow graph that were configured.
    /// </summary>
    private (Workflow, WorkflowGraph) SetupMaterializerAndGraphBuilder(WorkflowDefinition definition)
    {
        var workflow = new Workflow { Identity = new(definition.DefinitionId, 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);

        _materializerRegistry.GetMaterializer(definition.MaterializerName).Returns(materializer);
        _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);

        return (workflow, workflowGraph);
    }

    /// <summary>
    /// Sets up complete mocking chain for workflow graph retrieval including definition store, materializer, and graph builder.
    /// Returns the workflow graph that was configured.
    /// </summary>
    private WorkflowGraph SetupCompleteWorkflowGraphMocking(
        WorkflowDefinition definition,
        bool materializerAvailable = true,
        WorkflowDefinitionFilter? filter = null)
    {
        var workflow = new Workflow { Identity = new(definition.DefinitionId, 1, "v1") };
        var workflowGraph = TestHelpers.CreateWorkflowGraph(workflow);

        if (filter != null)
        {
            _workflowDefinitionStore.FindAsync(filter, Arg.Any<CancellationToken>()).Returns(definition);
        }
        else
        {
            _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
                .Returns(definition);
        }

        _materializerRegistry.IsMaterializerAvailable(definition.MaterializerName).Returns(materializerAvailable);

        if (materializerAvailable)
        {
            var materializer = Substitute.For<IWorkflowMaterializer>();
            materializer.MaterializeAsync(definition, Arg.Any<CancellationToken>()).Returns(workflow);
            _materializerRegistry.GetMaterializer(definition.MaterializerName).Returns(materializer);
            _workflowGraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(workflowGraph);
        }

        return workflowGraph;
    }
}
