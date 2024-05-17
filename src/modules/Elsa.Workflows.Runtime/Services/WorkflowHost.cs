using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a host of a workflow instance to interact with.
/// </summary>
public class WorkflowHost : IWorkflowHost
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WorkflowHost> _logger;
    private CancellationTokenSource? _linkedTokenSource;
    private Queue<RunWorkflowOptions?> _queuedRunWorkflowOptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHost"/> class.
    /// </summary>
    public WorkflowHost(
        IServiceScopeFactory serviceScopeFactory,
        WorkflowGraph workflowGraph,
        WorkflowState workflowState,
        ILogger<WorkflowHost> logger)
    {
        WorkflowGraph = workflowGraph;
        WorkflowState = workflowState;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public WorkflowGraph WorkflowGraph { get; }

    /// <inheritdoc />
    public Workflow Workflow => WorkflowGraph.Workflow;

    /// <inheritdoc />
    public WorkflowState WorkflowState { get; set; }

    /// <inheritdoc />
    public object? LastResult { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanStartWorkflowAsync(RunWorkflowOptions? @params = default, CancellationToken cancellationToken = default)
    {
        var strategyType = Workflow.Options.ActivationStrategyType;

        if (strategyType == null)
            return true;

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var instantiationStrategies = scope.ServiceProvider.GetServices<IWorkflowActivationStrategy>();
        var strategy = instantiationStrategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            return true;

        var correlationId = @params?.CorrelationId;
        var strategyContext = new WorkflowInstantiationStrategyContext(Workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowResult> RunWorkflowAsync(RunWorkflowOptions? @params = default, CancellationToken cancellationToken = default)
    {
        // Re-entrancy guard. If the workflow is currently executing, queue the request and return immediately.
        if(_linkedTokenSource != null)
        {
            _queuedRunWorkflowOptions.Enqueue(@params);
            return new RunWorkflowResult(WorkflowState, Workflow, null);
        }
        
        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new RunWorkflowResult(WorkflowState, Workflow, null);
        }
        
        var runOptions = new RunWorkflowOptions
        {
            WorkflowInstanceId = WorkflowState.Id,
            CorrelationId = @params?.CorrelationId,
            Input = @params?.Input,
            Properties = @params?.Properties,
            ActivityHandle = @params?.ActivityHandle,
            BookmarkId = @params?.BookmarkId,
            TriggerActivityId = @params?.TriggerActivityId,
            ParentWorkflowInstanceId = @params?.ParentWorkflowInstanceId
        };
        _linkedTokenSource = new CancellationTokenSource();
        var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _linkedTokenSource.Token).Token;

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(WorkflowGraph, WorkflowState, runOptions, linkedCancellationToken);

        WorkflowState = workflowResult.WorkflowState;
        await PersistStateAsync(scope, cancellationToken);
        
        _linkedTokenSource.Dispose();
        _linkedTokenSource = null;
        
        if (_queuedRunWorkflowOptions.Count > 0)
        {
            var nextRunOptions = _queuedRunWorkflowOptions.Dequeue();
            return await RunWorkflowAsync(nextRunOptions, cancellationToken);
        }
        
        return workflowResult;
    }

    /// <inheritdoc />
    public async Task CancelWorkflowAsync(CancellationToken cancellationToken = default)
    {
        // If the workflow is currently executing, cancel it. This will cause the workflow to be cancelled gracefully.
        if (_linkedTokenSource != null)
        {
            _linkedTokenSource.Cancel();
            return;
        }
        
        // Otherwise, cancel the workflow by executing the canceler. This will set up a workflow execution pipeline that will cancel the workflow.
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowCanceler = serviceProvider.GetRequiredService<IWorkflowCanceler>();
        WorkflowState = await workflowCanceler.CancelWorkflowAsync(WorkflowGraph, WorkflowState, cancellationToken);
        await PersistStateAsync(scope, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PersistStateAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        await PersistStateAsync(scope, cancellationToken);
    }

    private async Task PersistStateAsync(IServiceScope scope, CancellationToken cancellationToken = default)
    {
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, cancellationToken);
    }
}