using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class LocalWorkflowClientTests
{
    private readonly IWorkflowInstanceManager _workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
    private readonly IWorkflowDefinitionService _workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
    private readonly IWorkflowRunner _workflowRunner = Substitute.For<IWorkflowRunner>();
    private readonly IWorkflowCanceler _workflowCanceler = Substitute.For<IWorkflowCanceler>();
    private readonly IWorkflowActivationGate _workflowActivationGate = Substitute.For<IWorkflowActivationGate>();
    private readonly WorkflowStateMapper _workflowStateMapper = Substitute.For<WorkflowStateMapper>();
    private readonly ILogger<LocalWorkflowClient> _logger = Substitute.For<ILogger<LocalWorkflowClient>>();

    [Fact]
    public async Task CreateInstanceAsync_ThrowsWorkflowDefinitionNotFoundException_WhenDefinitionDoesNotExist()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("non-existent-definition");
        var request = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var findResult = new WorkflowGraphFindResult(null, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowDefinitionNotFoundException>(() =>
            client.CreateInstanceAsync(request));
    }

    [Fact]
    public async Task CreateInstanceAsync_ThrowsWorkflowMaterializerNotFoundException_WhenMaterializerNotAvailable()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition");
        var request = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var definition = new WorkflowDefinition
        {
            Id = "def-1",
            DefinitionId = "test-definition",
            MaterializerName = "unavailable-materializer"
        };

        var findResult = new WorkflowGraphFindResult(definition, null); // Null graph indicates materializer not available

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowMaterializerNotFoundException>(() => client.CreateInstanceAsync(request));
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_ThrowsWorkflowDefinitionNotFoundException_WhenDefinitionDoesNotExist()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("non-existent-definition");
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var findResult = new WorkflowGraphFindResult(null, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowDefinitionNotFoundException>(() =>
            client.CreateAndRunInstanceAsync(request));
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_ThrowsWorkflowMaterializerNotFoundException_WhenMaterializerNotAvailable()
    {
        // Arrange
        var client = CreateClient();
        var definitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition");
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = definitionHandle
        };

        var definition = new WorkflowDefinition
        {
            Id = "def-1",
            DefinitionId = "test-definition",
            MaterializerName = "unavailable-materializer"
        };

        var findResult = new WorkflowGraphFindResult(definition, null);

        _workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, Arg.Any<CancellationToken>())
            .Returns(findResult);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowMaterializerNotFoundException>(() =>
            client.CreateAndRunInstanceAsync(request));
    }

    [Fact]
    public async Task CreateInstanceAsync_ReturnsCannotStart_WhenActivationGateDenies()
    {
        var client = CreateClient(canStart: false);
        var request = new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition"),
            CorrelationId = "order-1"
        };
        SetupWorkflowGraph("test-definition");

        var response = await client.CreateInstanceAsync(request);

        Assert.True(response.CannotStart);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_ReturnsCannotStart_WhenActivationGateDenies()
    {
        var client = CreateClient(canStart: false);
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition"),
            CorrelationId = "order-1"
        };
        SetupWorkflowGraph("test-definition");

        var response = await client.CreateAndRunInstanceAsync(request);

        Assert.True(response.CannotStart);
        Assert.Null(response.WorkflowInstanceId);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().CreateAndCommitWorkflowInstanceAsync(default!, default, default);
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_DoesNotPersistProvisionalInstance_WhenRunnerThrows()
    {
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowInstanceManager.CreateWorkflowInstance(Arg.Any<Workflow>(), Arg.Any<WorkflowInstanceOptions>())
            .Returns(CreateRunningInstance());
        _workflowRunner.RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<RunWorkflowResult>(new InvalidOperationException("execution failed")));
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition"),
            CorrelationId = "correlation-1"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateAndRunInstanceAsync(request));

        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().CreateAndCommitWorkflowInstanceAsync(default!, default, default);
    }

    [Fact]
    public async Task CreateAndRunInstanceAsync_DoesNotPersistProvisionalInstance_WhenCancelledDuringRun()
    {
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowInstanceManager.CreateWorkflowInstance(Arg.Any<Workflow>(), Arg.Any<WorkflowInstanceOptions>())
            .Returns(CreateRunningInstance());
        _workflowRunner.RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<RunWorkflowResult>(call.Arg<CancellationToken>()));
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition"),
            CorrelationId = "correlation-1"
        };
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CreateAndRunInstanceAsync(request, cancellationSource.Token));

        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().CreateAndCommitWorkflowInstanceAsync(default!, default, default);
    }

    private static WorkflowInstance CreateRunningInstance() => new()
    {
        Id = "test-workflow-instance-id",
        DefinitionId = "test-definition",
        DefinitionVersionId = "test-definition",
        Status = WorkflowStatus.Running,
        WorkflowState = new WorkflowState { Id = "test-workflow-instance-id", Status = WorkflowStatus.Running }
    };

    private void SetupWorkflowGraph(string definitionId)
    {
        var workflow = new Workflow
        {
            Id = definitionId,
            Identity = new WorkflowIdentity(definitionId, 1, definitionId)
        };
        var node = new ActivityNode(workflow, "Root");
        var graph = new WorkflowGraph(workflow, node, [node]);
        var definition = new WorkflowDefinition
        {
            Id = definitionId,
            DefinitionId = definitionId
        };

        _workflowDefinitionService.TryFindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionHandle>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowGraphFindResult(definition, graph));
    }

    private LocalWorkflowClient CreateClient(bool canStart = true)
    {
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(canStart ? new WorkflowActivationLease(true, null) : WorkflowActivationLease.Denied);

        return new(
            "test-workflow-instance-id",
            _workflowInstanceManager,
            _workflowDefinitionService,
            _workflowRunner,
            _workflowCanceler,
            _workflowActivationGate,
            _workflowStateMapper,
            _logger);
    }
}
