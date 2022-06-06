using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Implementations;

public class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IActivityWalker _activityWalker;
    private readonly IWorkflowExecutionPipeline _pipeline;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IIdentityGenerator _identityGenerator;

    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IActivityWalker activityWalker,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateSerializer workflowStateSerializer,
        IIdentityGraphService identityGraphService,
        IActivitySchedulerFactory schedulerFactory,
        IIdentityGenerator identityGenerator)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _activityWalker = activityWalker;
        _pipeline = pipeline;
        _workflowStateSerializer = workflowStateSerializer;
        _identityGraphService = identityGraphService;
        _schedulerFactory = schedulerFactory;
        _identityGenerator = identityGenerator;
    }

    public async Task<InvokeWorkflowResult> RunAsync(Workflow workflow, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Setup a workflow execution context.
        var workflowExecutionContext = CreateWorkflowExecutionContext(scope.ServiceProvider, workflow, default, default, input, default, cancellationToken);

        // Schedule the first node.
        workflowExecutionContext.ScheduleRoot();

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = CreateWorkflowExecutionContext(scope.ServiceProvider, workflow, workflowState, default, input, default, cancellationToken);
        
        // Schedule the first node.
        workflowExecutionContext.ScheduleRoot();
        
        return await RunAsync(workflowExecutionContext);
    }

    public async Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = CreateWorkflowExecutionContext(scope.ServiceProvider, workflow, workflowState, bookmark, input, default, cancellationToken);

        if (bookmark != null) 
            workflowExecutionContext.ScheduleBookmark(bookmark);

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<InvokeWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        // Transition into the Running state.
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);
        
        // Execute the activity execution pipeline.
        await _pipeline.ExecuteAsync(workflowExecutionContext);

        // Extract workflow state.
        var workflowState = _workflowStateSerializer.SerializeState(workflowExecutionContext);

        // Return workflow execution result containing state + bookmarks.
        return new InvokeWorkflowResult(workflowState, workflowExecutionContext.Bookmarks);
    }

    private WorkflowExecutionContext CreateWorkflowExecutionContext(
        IServiceProvider serviceProvider,
        Workflow workflow,
        WorkflowState? workflowState,
        Bookmark? bookmark,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeActivityDelegate,
        CancellationToken cancellationToken)
    {
        var root = workflow.Root;

        // Build graph.
        var graph = _activityWalker.Walk(root);

        // Assign identities.
        _identityGraphService.AssignIdentities(graph);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var id = workflowState?.Id ?? _identityGenerator.GenerateId();
        var correlationId = workflowState?.CorrelationId;
        var workflowExecutionContext = new WorkflowExecutionContext(serviceProvider, id, correlationId, workflow, graph, scheduler, bookmark, input, executeActivityDelegate, cancellationToken);

        // Restore workflow execution context from state, if provided.
        if (workflowState != null) _workflowStateSerializer.DeserializeState(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}