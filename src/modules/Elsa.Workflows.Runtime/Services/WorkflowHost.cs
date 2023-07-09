using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a host of a workflow instance to interact with.
/// </summary>
public class WorkflowHost : IWorkflowHost
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IEnumerable<IWorkflowActivationStrategy> _instantiationStrategies;
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
        IIdentityGenerator identityGenerator,
        ILogger<WorkflowHost> logger)
    {
        Workflow = workflow;
        WorkflowState = workflowState;
        _workflowRunner = workflowRunner;
        _instantiationStrategies = instantiationStrategies;
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
        var originalBookmarks = WorkflowState.Bookmarks.ToList();
        var input = options?.Input;
        var runOptions = new RunWorkflowOptions(instanceId, correlationId, input: input, triggerActivityId: options?.TriggerActivityId);
        var workflowResult = await _workflowRunner.RunAsync(Workflow, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);

        return new StartWorkflowHostResult(diff);
    }

    /// <summary>
    /// Resumes the <see cref="Workflow"/>.
    /// </summary>
    public async Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var originalBookmarks = WorkflowState.Bookmarks.ToList();

        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, new List<Bookmark>()));
        }

        var instanceId = WorkflowState.Id;
        var input = options?.Input;

        var runOptions = new RunWorkflowOptions(
            instanceId,
            options?.CorrelationId,
            options?.BookmarkId,
            options?.ActivityId,
            options?.ActivityNodeId,
            options?.ActivityInstanceId,
            options?.ActivityHash,
            input
        );

        var workflowResult = await _workflowRunner.RunAsync(Workflow, WorkflowState, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, updatedBookmarks));
    }
}