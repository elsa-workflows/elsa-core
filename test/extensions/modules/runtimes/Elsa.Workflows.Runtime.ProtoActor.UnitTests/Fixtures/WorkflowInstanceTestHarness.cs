using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.Mappers;
using Elsa.Workflows.State;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Proto;
using Proto.Cluster;
using ProtoActorMappers = Elsa.Workflows.Runtime.ProtoActor.Mappers.Mappers;
using ProtoImportWorkflowStateRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.ImportWorkflowStateRequest;
using ProtoRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceRequest;
using ProtoWorkflowInstanceActor = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.WorkflowInstanceActor;
using WorkflowInstanceActorImplementation = Elsa.Workflows.Runtime.ProtoActor.Actors.WorkflowInstance;
using WorkflowInstanceEntity = Elsa.Workflows.Management.Entities.WorkflowInstance;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;

internal sealed class WorkflowInstanceTestHarness : IAsyncDisposable
{
    public const string DefinitionId = "definition";
    public const string DefinitionVersionId = "definition-version";
    private const string InstanceId = "instance";
    private const int RunMethodIndex = 1;
    private const int ImportStateMethodIndex = 7;

    private readonly ActorSystem _actorSystem = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly PID _workflowInstanceActor;
    private readonly Mock<IWorkflowStateSerializer> _workflowStateSerializer = new();

    public WorkflowInstanceTestHarness()
    {
        WorkflowGraph = CreateWorkflowGraph(DefinitionVersionId, 1);
        WorkflowDefinitionService
            .Setup(x => x.FindWorkflowGraphAsync(It.IsAny<WorkflowDefinitionHandle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowGraph);
        WorkflowInstanceManager
            .Setup(x => x.SaveAsync(It.IsAny<WorkflowState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowState state, CancellationToken _) => CreateWorkflowInstance(state));
        WorkflowRunner
            .Setup(x => x.RunAsync(It.IsAny<WorkflowGraph>(), It.IsAny<WorkflowState>(), It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowGraph graph, WorkflowState state, RunWorkflowOptions? _, CancellationToken _) =>
                new RunWorkflowResult(null!, state, graph.Workflow, null, Journal.Empty));

        var services = new ServiceCollection();
        services.AddSingleton(WorkflowRunner.Object);
        services.AddSingleton(WorkflowInstanceManager.Object);
        services.AddSingleton(WorkflowDefinitionService.Object);
        _serviceProvider = services.BuildServiceProvider();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var mappers = CreateMappers();
        var props = Props.FromProducer(() => new ProtoWorkflowInstanceActor(
            (context, _) => new WorkflowInstanceActorImplementation(context, scopeFactory, mappers)));
        _workflowInstanceActor = _actorSystem.Root.Spawn(
            props,
            context => context.Set(ClusterIdentity.Create(InstanceId, ProtoWorkflowInstanceActor.Kind)));
    }

    public Mock<IWorkflowRunner> WorkflowRunner { get; } = new();
    public Mock<IWorkflowInstanceManager> WorkflowInstanceManager { get; } = new();
    public Mock<IWorkflowDefinitionService> WorkflowDefinitionService { get; } = new();
    public WorkflowGraph WorkflowGraph { get; }

    public static WorkflowState CreateState(string definitionVersionId = DefinitionVersionId, int definitionVersion = 1) => new()
    {
        Id = InstanceId,
        DefinitionId = DefinitionId,
        DefinitionVersionId = definitionVersionId,
        DefinitionVersion = definitionVersion,
        Status = WorkflowStatus.Running,
        SubStatus = WorkflowSubStatus.Suspended
    };

    public static WorkflowGraph CreateWorkflowGraph(string definitionVersionId, int definitionVersion)
    {
        var workflow = new Workflow
        {
            Id = $"workflow-{definitionVersion}",
            Identity = new(DefinitionId, definitionVersion, definitionVersionId)
        };
        var root = new ActivityNode(workflow, "");
        return new(workflow, root, [root]);
    }

    public void SetupExistingInstance(WorkflowState state) => WorkflowInstanceManager
        .Setup(x => x.FindByIdAsync(InstanceId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(CreateWorkflowInstance(state));

    public Task<object> ImportStateAsync(WorkflowState state, CancellationToken cancellationToken)
    {
        var serializedState = Guid.NewGuid().ToString("N");
        _workflowStateSerializer.Setup(x => x.Deserialize(serializedState)).Returns(state);
        var request = new ProtoImportWorkflowStateRequest
        {
            SerializedWorkflowState = new() { Text = serializedState }
        };
        return RequestAsync(ImportStateMethodIndex, request, cancellationToken);
    }

    public Task<object> RunAsync(CancellationToken cancellationToken) => RequestAsync(
        RunMethodIndex,
        new ProtoRunWorkflowInstanceRequest { ActivityHandle = new() },
        cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _actorSystem.Root.PoisonAsync(_workflowInstanceActor);
        await _actorSystem.ShutdownAsync();
        await _serviceProvider.DisposeAsync();
    }

    private async Task<object> RequestAsync(int methodIndex, IMessage message, CancellationToken cancellationToken)
    {
        using var future = _actorSystem.Root.GetFuture();
        _actorSystem.Root.Request(_workflowInstanceActor, new GrainRequestMessage(methodIndex, message), future.Pid);
        return await future.GetTask(cancellationToken);
    }

    private static WorkflowInstanceEntity CreateWorkflowInstance(WorkflowState state) => new()
    {
        Id = state.Id,
        DefinitionId = state.DefinitionId,
        DefinitionVersionId = state.DefinitionVersionId,
        Version = state.DefinitionVersion,
        WorkflowState = state,
        Status = state.Status,
        SubStatus = state.SubStatus
    };

    private ProtoActorMappers CreateMappers()
    {
        var activityHandleMapper = new ActivityHandleMapper();
        var workflowDefinitionHandleMapper = new WorkflowDefinitionHandleMapper();
        var exceptionMapper = new ExceptionMapper();
        var activityIncidentMapper = new ActivityIncidentMapper(exceptionMapper);
        var workflowStatusMapper = new WorkflowStatusMapper();
        var workflowSubStatusMapper = new WorkflowSubStatusMapper();

        return new(
            activityHandleMapper,
            workflowDefinitionHandleMapper,
            activityIncidentMapper,
            exceptionMapper,
            workflowStatusMapper,
            workflowSubStatusMapper,
            new(workflowDefinitionHandleMapper),
            new(),
            new(activityHandleMapper),
            new(workflowStatusMapper, workflowSubStatusMapper, activityIncidentMapper),
            new(workflowDefinitionHandleMapper, activityHandleMapper),
            new(activityHandleMapper),
            new(_workflowStateSerializer.Object));
    }
}
