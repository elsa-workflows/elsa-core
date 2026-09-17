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
using Elsa.Workflows.Runtime.ActivationValidators;
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
    public async Task CreateInstanceAsync_DoesNotPersist_WhenDistributedLeaseIsLostBeforeSave()
    {
        using var handleLostSource = new CancellationTokenSource();
        var lockHandle = new TrackingAsyncDisposable();
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                handleLostSource.Cancel();
                return Task.FromResult(new WorkflowActivationLease(true, lockHandle, handleLostSource.Token));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition")
        }));

        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
        Assert.Equal(1, lockHandle.DisposeCount);
    }

    [Fact]
    public async Task CreateInstanceAsync_CancelsPersistence_WhenDistributedLeaseIsLostDuringSave()
    {
        using var handleLostSource = new CancellationTokenSource();
        var lockHandle = new TrackingAsyncDisposable();
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new WorkflowActivationLease(true, lockHandle, handleLostSource.Token)));
        var saveStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _workflowInstanceManager.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var cancellationToken = call.Arg<CancellationToken>();
                saveStarted.SetResult(cancellationToken);
                var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellationToken.Register(() => canceled.TrySetCanceled(cancellationToken));
                return canceled.Task;
            });

        var save = client.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition")
        });
        var persistenceToken = await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        handleLostSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => save);
        Assert.True(persistenceToken.IsCancellationRequested);
        await _workflowInstanceManager.Received(1).SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, lockHandle.DisposeCount);
    }

    [Fact]
    public async Task CreateInstanceAsync_PreservesCallerCancellation_WhenActivationGateReturnsLegacyLease()
    {
        using var cancellationSource = new CancellationTokenSource();
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkflowActivationLease(true, null)));
        var saveStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        _workflowInstanceManager.SaveAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var cancellationToken = call.Arg<CancellationToken>();
                saveStarted.SetResult(cancellationToken);
                var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellationToken.Register(() => canceled.TrySetCanceled(cancellationToken));
                return canceled.Task;
            });

        var save = client.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition")
        }, cancellationSource.Token);
        var persistenceToken = await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(cancellationSource.Token, persistenceToken);
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => save);
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
    public async Task CreateAndRunInstanceAsync_LogsSafeStructuredActivationDenialDetails()
    {
        const string correlationId = "sensitive-correlation-123";
        var logger = new RecordingLogger<LocalWorkflowClient>();
        var client = CreateClient(canStart: false, logger);
        SetupWorkflowGraph("test-definition", typeof(CorrelatedSingletonStrategy));
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition"),
            CorrelationId = correlationId
        };

        var response = await client.CreateAndRunInstanceAsync(request);

        Assert.True(response.CannotStart);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().SaveAsync(default(WorkflowInstance)!, default);
        await _workflowInstanceManager.DidNotReceiveWithAnyArgs().CreateAndCommitWorkflowInstanceAsync(default!, default, default);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("test-definition", entry.Properties["WorkflowDefinitionId"]);
        Assert.Equal("test-definition", entry.Properties["WorkflowDefinitionVersionId"]);
        Assert.Equal(1, entry.Properties["WorkflowDefinitionVersion"]);
        Assert.Equal(typeof(CorrelatedSingletonStrategy).FullName, entry.Properties["ActivationStrategyType"]);
        Assert.Equal(true, entry.Properties["CorrelationIdPresent"]);
        Assert.DoesNotContain(correlationId, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(correlationId, string.Join(";", entry.Properties.Values), StringComparison.Ordinal);
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

    [Fact]
    public async Task CreateAndRunInstanceAsync_CancelsRunner_WhenDistributedLeaseIsLostDuringRun()
    {
        using var handleLostSource = new CancellationTokenSource();
        var lockHandle = new TrackingAsyncDisposable();
        var client = CreateClient();
        SetupWorkflowGraph("test-definition");
        _workflowInstanceManager.CreateWorkflowInstance(Arg.Any<Workflow>(), Arg.Any<WorkflowInstanceOptions>())
            .Returns(CreateRunningInstance());
        var runnerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runnerCompletion = new TaskCompletionSource<RunWorkflowResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken runnerToken = default;
        _workflowRunner.RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                runnerToken = call.Arg<CancellationToken>();
                runnerToken.Register(() => runnerCompletion.TrySetCanceled(runnerToken));
                runnerStarted.SetResult();
                return runnerCompletion.Task;
            });
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new WorkflowActivationLease(true, lockHandle, handleLostSource.Token)));

        var run = client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("test-definition")
        });
        await runnerStarted.Task;
        handleLostSource.Cancel();
        var runnerObservedLeaseLoss = runnerToken.IsCancellationRequested;
        if (!runnerObservedLeaseLoss)
            runnerCompletion.TrySetException(new InvalidOperationException("The runner did not receive the lease-loss cancellation token."));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(runnerObservedLeaseLoss);
        Assert.Equal(1, lockHandle.DisposeCount);
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

    private void SetupWorkflowGraph(string definitionId, Type? activationStrategyType = null)
    {
        var workflow = new Workflow
        {
            Id = definitionId,
            Identity = new WorkflowIdentity(definitionId, 1, definitionId),
            Options = new WorkflowOptions { ActivationStrategyType = activationStrategyType }
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

    private LocalWorkflowClient CreateClient(bool canStart = true, ILogger<LocalWorkflowClient>? logger = null)
    {
        _workflowActivationGate.EvaluateAsync(Arg.Any<Workflow>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => canStart
                ? new WorkflowActivationLease(true, null, call.Arg<CancellationToken>())
                : WorkflowActivationLease.Denied);

        return new(
            "test-workflow-instance-id",
            _workflowInstanceManager,
            _workflowDefinitionService,
            _workflowRunner,
            _workflowCanceler,
            _workflowActivationGate,
            _workflowStateMapper,
            logger ?? _logger);
    }

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();
            Entries.Add(new(logLevel, formatter(state, exception), properties));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);
}
