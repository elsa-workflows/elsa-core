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
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IActivitySchedulerFactory _schedulerFactory;
    private readonly IIdentityGenerator _identityGenerator;

    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IActivityWalker activityWalker,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateSerializer workflowStateSerializer,
        IIdentityGraphService identityGraphService,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IActivitySchedulerFactory schedulerFactory,
        IIdentityGenerator identityGenerator)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _activityWalker = activityWalker;
        _pipeline = pipeline;
        _workflowStateSerializer = workflowStateSerializer;
        _identityGraphService = identityGraphService;
        _workflowBuilderFactory = workflowBuilderFactory;
        _schedulerFactory = schedulerFactory;
        _identityGenerator = identityGenerator;
    }

    public async Task<RunWorkflowResult> RunAsync(IActivity activity, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var instanceId = _identityGenerator.GenerateId();
        return await RunAsync(instanceId, activity, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(IWorkflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var instanceId = _identityGenerator.GenerateId();
        return await RunAsync(workflow, instanceId, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync<T>(IDictionary<string, object>? input = default, CancellationToken cancellationToken = default) where T : IWorkflow
    {
        var instanceId = _identityGenerator.GenerateId();
        return await RunAsync<T>(instanceId, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(string instanceId, IActivity activity, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var workflow = Workflow.FromActivity(activity);
        return await RunAsync(workflow, instanceId, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(IWorkflow workflow, string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync(workflow, cancellationToken);
        return await RunAsync(workflowDefinition, instanceId, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync<T>(string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default) where T : IWorkflow
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        return await RunAsync(workflowDefinition, instanceId, input, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, string instanceId, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Setup a workflow execution context.
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(scope.ServiceProvider, workflow, instanceId, default, default, input, default, cancellationToken);

        // Schedule the first activity.
        workflowExecutionContext.ScheduleRoot();

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(scope.ServiceProvider, workflow, workflowState.Id, workflowState, default, input, default, cancellationToken);
        
        // Schedule the first node.
        workflowExecutionContext.ScheduleRoot();
        
        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(scope.ServiceProvider, workflow, workflowState.Id, workflowState, bookmark, input, default, cancellationToken);

        if (bookmark != null) 
            workflowExecutionContext.ScheduleBookmark(bookmark);

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        // Transition into the Running state.
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);
        
        // Execute the activity execution pipeline.
        await _pipeline.ExecuteAsync(workflowExecutionContext);

        // Extract workflow state.
        var workflowState = _workflowStateSerializer.SerializeState(workflowExecutionContext);

        // Return workflow execution result containing state + bookmarks.
        return new RunWorkflowResult(workflowState, workflowExecutionContext.Bookmarks);
    }

    private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(
        IServiceProvider serviceProvider,
        Workflow workflow,
        string instanceId,
        WorkflowState? workflowState,
        Bookmark? bookmark,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeActivityDelegate,
        CancellationToken cancellationToken)
    {
        var root = workflow.Root;

        // Build graph.
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);

        // Assign identities.
        _identityGraphService.AssignIdentities(graph);

        // Create scheduler.
        var scheduler = _schedulerFactory.CreateScheduler();

        // Setup a workflow execution context.
        var correlationId = workflowState?.CorrelationId;
        var workflowExecutionContext = new WorkflowExecutionContext(serviceProvider, instanceId, correlationId, workflow, graph, scheduler, bookmark, input, executeActivityDelegate, cancellationToken);

        // Restore workflow execution context from state, if provided.
        if (workflowState != null) _workflowStateSerializer.DeserializeState(workflowExecutionContext, workflowState);

        return workflowExecutionContext;
    }
}