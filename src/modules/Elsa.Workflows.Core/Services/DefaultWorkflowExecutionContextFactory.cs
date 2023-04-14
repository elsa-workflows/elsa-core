using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class DefaultWorkflowExecutionContextFactory : IWorkflowExecutionContextFactory
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IWorkflowExecutionContextMapper _workflowExecutionContextMapper;
    private readonly IHasher _hasher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowExecutionContextFactory(
        IActivityVisitor activityVisitor,
        IIdentityGraphService identityGraphService,
        IActivitySchedulerFactory schedulerFactory,
        IActivityRegistry activityRegistry,
        IWorkflowExecutionContextMapper workflowExecutionContextMapper,
        IHasher hasher)
    {
        _activityVisitor = activityVisitor;
        _identityGraphService = identityGraphService;
        _schedulerFactory = schedulerFactory;
        _activityRegistry = activityRegistry;
        _workflowExecutionContextMapper = workflowExecutionContextMapper;
        _hasher = hasher;
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
        var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
        var flattenedList = graph.Flatten().ToList();
        
        // Register activity types.
        var activityTypes = flattenedList.Select(x => x.Activity.GetType()).Distinct().ToList();
        await _activityRegistry.RegisterAsync(activityTypes, cancellationToken);
        
        var needsIdentityAssignment = flattenedList.Any(x => string.IsNullOrEmpty(x.Activity.Id));

        if (needsIdentityAssignment)
            _identityGraphService.AssignIdentities(flattenedList);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var workflowExecutionContext = new WorkflowExecutionContext(
            serviceProvider,
            _hasher,
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
        if (workflowState != null) _workflowExecutionContextMapper.Apply(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}