using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class DefaultWorkflowExecutionContextFactory : IWorkflowExecutionContextFactory
{
    private readonly IActivityWalker _activityWalker;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowExecutionContextFactory(
        IActivityWalker activityWalker,
        IIdentityGraphService identityGraphService,
        IActivitySchedulerFactory schedulerFactory,
        IActivityRegistry activityRegistry,
        IWorkflowStateSerializer workflowStateSerializer)
    {
        _activityWalker = activityWalker;
        _identityGraphService = identityGraphService;
        _schedulerFactory = schedulerFactory;
        _activityRegistry = activityRegistry;
        _workflowStateSerializer = workflowStateSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionContext> CreateAsync(
        IServiceProvider serviceProvider,
        Workflow workflow,
        string instanceId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input = default,
        string? correlationId = default,
        ExecuteActivityDelegate? executeActivityDelegate = default,
        string? triggerActivityId = default,
        CancellationToken cancellationToken = default)
    {
        var root = workflow;

        // Build graph.
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);
        var flattenedList = graph.Flatten().ToList();
        var needsIdentityAssignment = flattenedList.Any(x => string.IsNullOrEmpty(x.Activity.Id));

        if (needsIdentityAssignment)
            _identityGraphService.AssignIdentities(flattenedList);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var workflowExecutionContext = new WorkflowExecutionContext(
            serviceProvider,
            instanceId,
            correlationId,
            workflow,
            graph,
            flattenedList,
            scheduler,
            _activityRegistry,
            input,
            executeActivityDelegate,
            triggerActivityId,
            default,
            cancellationToken);

        // Restore workflow execution context from state, if provided.
        if (workflowState != null) _workflowStateSerializer.DeserializeState(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}