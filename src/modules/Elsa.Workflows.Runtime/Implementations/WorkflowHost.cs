using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Implementations;

/// <summary>
/// Represents a host of a workflow instance to interact with.
/// </summary>
public class WorkflowHost : IWorkflowHost
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IEnumerable<IWorkflowActivationStrategy> _instantiationStrategies;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ILogger<WorkflowHost> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowHost(
        Workflow workflow,
        WorkflowState workflowState,
        IWorkflowRunner workflowRunner,
        IEnumerable<IWorkflowActivationStrategy> instantiationStrategies,
        IEventPublisher eventPublisher,
        IIdentityGenerator identityGenerator,
        ILogger<WorkflowHost> logger)
    {
        Workflow = workflow;
        WorkflowState = workflowState;
        _workflowRunner = workflowRunner;
        _instantiationStrategies = instantiationStrategies;
        _eventPublisher = eventPublisher;
        _identityGenerator = identityGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Workflow Workflow { get; set; }
    
    /// <inheritdoc />
    public WorkflowState WorkflowState { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanStartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var strategyType = Workflow.Options?.ActivationStrategyType;

        if (strategyType == null)
            return true;

        var strategy = _instantiationStrategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            return true;

        var correlationId = options?.CorrelationId;
        var strategyContext = new WorkflowInstantiationStrategyContext(Workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }

    /// <inheritdoc />
    public async Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var correlationId = options?.CorrelationId;
        var instanceId = options?.InstanceId ?? _identityGenerator.GenerateId();
        await _eventPublisher.PublishAsync(new WorkflowExecuting(Workflow), cancellationToken);

        var originalBookmarks = WorkflowState?.Bookmarks.ToList() ?? new List<Bookmark>();
        var input = options?.Input;
        var runOptions = new RunWorkflowOptions(instanceId, correlationId, Input: input, TriggerActivityId: options?.TriggerActivityId);
        var workflowResult = await _workflowRunner.RunAsync(Workflow, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        
        await _eventPublisher.PublishAsync(new WorkflowExecuted(Workflow, WorkflowState), cancellationToken);

        return new StartWorkflowHostResult(diff);
    }

    /// <summary>
    /// Resumes the <see cref="Workflow"/>.
    /// </summary>
    public async Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        await _eventPublisher.PublishAsync(new WorkflowExecuting(Workflow), cancellationToken);

        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new ResumeWorkflowHostResult();
        }

        var instanceId = WorkflowState.Id;
        var input = options?.Input;
        var runOptions = new RunWorkflowOptions(instanceId, options?.CorrelationId, options?.BookmarkId, options?.ActivityId, input);
        var workflowResult = await _workflowRunner.RunAsync(Workflow, WorkflowState, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;
        await _eventPublisher.PublishAsync(new WorkflowExecuted(Workflow, WorkflowState), cancellationToken);

        return new ResumeWorkflowHostResult();
    }
}