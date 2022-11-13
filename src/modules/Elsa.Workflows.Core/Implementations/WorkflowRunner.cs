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
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;

    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IActivityWalker activityWalker,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateSerializer workflowStateSerializer,
        IIdentityGraphService identityGraphService,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IActivitySchedulerFactory schedulerFactory,
        IIdentityGenerator identityGenerator,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _activityWalker = activityWalker;
        _pipeline = pipeline;
        _workflowStateSerializer = workflowStateSerializer;
        _identityGraphService = identityGraphService;
        _workflowBuilderFactory = workflowBuilderFactory;
        _schedulerFactory = schedulerFactory;
        _identityGenerator = identityGenerator;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
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
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflow, instanceId, default, input, default, cancellationToken);

        // Schedule the first activity.
        workflowExecutionContext.ScheduleRoot();

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflow, workflowState.Id, workflowState, input, default, cancellationToken);

        // Schedule the first node.
        workflowExecutionContext.ScheduleRoot();

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, string? bookmarkId, IDictionary<string, object>? input, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflow, workflowState.Id, workflowState, input, default, cancellationToken);

        // Schedule the bookmark, if any.
        if (bookmarkId != null)
        {
            var bookmark = workflowExecutionContext.Bookmarks.FirstOrDefault(x => x.Id == bookmarkId);

            if (bookmark != null)
                workflowExecutionContext.ScheduleBookmark(bookmark);
        }

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        // Transition into the Running state.
        workflowExecutionContext.TransitionTo(WorkflowSubStatus.Executing);

        // Execute the workflow execution pipeline.
        await _pipeline.ExecuteAsync(workflowExecutionContext);

        // Extract workflow state.
        var workflowState = _workflowStateSerializer.SerializeState(workflowExecutionContext);

        // Return workflow execution result containing state + bookmarks.
        return new RunWorkflowResult(workflowState);
    }

    private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(
        Workflow workflow,
        string instanceId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeActivityDelegate,
        CancellationToken cancellationToken) =>
        await _workflowExecutionContextFactory.CreateAsync(workflow, instanceId, workflowState, input, executeActivityDelegate, cancellationToken);
}