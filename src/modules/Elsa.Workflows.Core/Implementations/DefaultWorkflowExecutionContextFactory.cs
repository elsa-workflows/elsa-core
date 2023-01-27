using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Implementations;

/// <inheritdoc />
public class DefaultWorkflowExecutionContextFactory : IWorkflowExecutionContextFactory
{
    private readonly IActivityWalker _activityWalker;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowExecutionContextFactory(
        IActivityWalker activityWalker,
        IIdentityGraphService identityGraphService,
        IActivitySchedulerFactory schedulerFactory,
        IWorkflowStateSerializer workflowStateSerializer,
        IServiceProvider serviceProvider)
    {
        _activityWalker = activityWalker;
        _identityGraphService = identityGraphService;
        _schedulerFactory = schedulerFactory;
        _workflowStateSerializer = workflowStateSerializer;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionContext> CreateAsync(
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
            _serviceProvider,
            instanceId,
            correlationId,
            workflow,
            graph,
            flattenedList,
            scheduler,
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