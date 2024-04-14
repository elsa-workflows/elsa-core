using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Results;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHost"/> class.
    /// </summary>
    public WorkflowHost(
        IServiceScopeFactory serviceScopeFactory,
        Workflow workflow,
        WorkflowState workflowState,
        ILogger<WorkflowHost> logger)
    {
        Workflow = workflow;
        WorkflowState = workflowState;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Workflow Workflow { get; set; }

    /// <inheritdoc />
    public WorkflowState WorkflowState { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanStartWorkflowAsync(IExecuteWorkflowRequest? @params = default, CancellationToken cancellationToken = default)
    {
        var strategyType = Workflow.Options.ActivationStrategyType;

        if (strategyType == null)
            return true;

        using var scope = _serviceScopeFactory.CreateScope();
        var instantiationStrategies = scope.ServiceProvider.GetServices<IWorkflowActivationStrategy>();
        var strategy = instantiationStrategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            return true;

        var correlationId = @params?.CorrelationId;
        var strategyContext = new WorkflowInstantiationStrategyContext(Workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteWorkflowAsync(IExecuteWorkflowRequest? @params = default, CancellationToken cancellationToken = default)
    {
        var originalBookmarks = WorkflowState.Bookmarks.ToBookmarkInfos().ToList();

        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new ExecuteWorkflowResult(WorkflowState.Id, Diff.For(originalBookmarks, new List<BookmarkInfo>()), WorkflowState.Status, WorkflowState.SubStatus, WorkflowState.Incidents);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
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
        var workflowResult = await workflowRunner.RunAsync(Workflow, WorkflowState, runOptions, linkedCancellationToken);

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = @params?.IsExistingInstance == true
            ? await workflowRunner.RunAsync(Workflow, WorkflowState, runOptions, cancellationToken)
            : await workflowRunner.RunAsync(Workflow, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;
        await PersistStateAsync(scope, cancellationToken);

        var updatedBookmarks = WorkflowState.Bookmarks.ToBookmarkInfos().ToList();
        _linkedTokenSource.Dispose();
        _linkedTokenSource = null;
        
        return new ExecuteWorkflowResult(WorkflowState.Id, Diff.For(originalBookmarks, updatedBookmarks), WorkflowState.Status, WorkflowState.SubStatus, WorkflowState.Incidents);
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
        
        // Otherwise, cancel the workflow by executing the canceler. This will setup a workflow execution pipeline that will cancel the workflow.
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var workflowCanceler = serviceProvider.GetRequiredService<IWorkflowCanceler>();
        WorkflowState = await workflowCanceler.CancelWorkflowAsync(Workflow, WorkflowState, cancellationToken);
        await PersistStateAsync(scope, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PersistStateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        await PersistStateAsync(scope, cancellationToken);
    }

    private async Task PersistStateAsync(IServiceScope scope, CancellationToken cancellationToken = default)
    {
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PersistStateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, cancellationToken);
    }
}