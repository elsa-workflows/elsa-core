using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Implementations;

public class WorkflowRunner : IWorkflowRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWorkflowExecutionPipeline _pipeline;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;

    public WorkflowRunner(
        IServiceScopeFactory serviceScopeFactory,
        IWorkflowExecutionPipeline pipeline,
        IWorkflowStateSerializer workflowStateSerializer,
        IWorkflowBuilderFactory workflowBuilderFactory,
        IIdentityGenerator identityGenerator,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _pipeline = pipeline;
        _workflowStateSerializer = workflowStateSerializer;
        _workflowBuilderFactory = workflowBuilderFactory;
        _identityGenerator = identityGenerator;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
    }

    public async Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var workflow = Workflow.FromActivity(activity);
        return await RunAsync(workflow, options, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync(workflow, cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : IWorkflow
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflowDefinition = await builder.BuildWorkflowAsync<T>(cancellationToken);
        return await RunAsync(workflowDefinition, options, cancellationToken);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Setup a workflow execution context.
        var instanceId = options?.InstanceId ?? _identityGenerator.GenerateId();
        var input = options?.Input;
        var correlationId = options?.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflow, instanceId, correlationId, default, input, default, triggerActivityId, cancellationToken);

        // Schedule the first activity.
        workflowExecutionContext.ScheduleRoot();

        return await RunAsync(workflowExecutionContext);
    }

    public async Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default)
    {
        // Create a child scope.
        using var scope = _serviceScopeFactory.CreateScope();

        // Create workflow execution context.
        var input = options?.Input;
        var correlationId = options?.CorrelationId ?? workflowState.CorrelationId;
        var triggerActivityId = options?.TriggerActivityId;
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(workflow, workflowState.Id, correlationId, workflowState, input, default, triggerActivityId, cancellationToken);

        var bookmarkId = options?.BookmarkId;

        if (bookmarkId == null)
        {
            // Schedule the first node.
            workflowExecutionContext.ScheduleRoot();
        }
        else
        {
            // Schedule the bookmark.
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
        string? correlationId,
        WorkflowState? workflowState,
        IDictionary<string, object>? input,
        ExecuteActivityDelegate? executeActivityDelegate,
        string? triggerActivityId,
        CancellationToken cancellationToken) =>
        await _workflowExecutionContextFactory.CreateAsync(workflow, instanceId, workflowState, input, correlationId, executeActivityDelegate, triggerActivityId, cancellationToken);
}