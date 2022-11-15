using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Implementations;

public class DefaultWorkflowExecutionContextFactory : IWorkflowExecutionContextFactory
{
    private readonly IActivityWalker _activityWalker;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IServiceProvider _serviceProvider;

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
    
    public async Task<WorkflowExecutionContext> CreateAsync(
        Workflow workflow, 
        string instanceId,
        WorkflowState? workflowState, 
        IDictionary<string, object>? input = default,
        string? correlationId = default,
        ExecuteActivityDelegate? executeActivityDelegate = default, 
        CancellationToken cancellationToken = default)
    {
        var root = workflow;

        // Build graph.
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);

        // Assign identities.
        _identityGraphService.AssignIdentities(graph);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var workflowExecutionContext = new WorkflowExecutionContext(_serviceProvider, instanceId, correlationId, workflow, graph, scheduler, input, executeActivityDelegate, cancellationToken);

        // Restore workflow execution context from state, if provided.
        if (workflowState != null) _workflowStateSerializer.DeserializeState(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}