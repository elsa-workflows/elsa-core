using Elsa.Mediator.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCanceler(
    IWorkflowExecutionPipeline workflowExecutionPipeline, 
    IWorkflowStateExtractor workflowStateExtractor, 
    IMediator mediator, 
    IServiceProvider serviceProvider) : IWorkflowCanceler
{
    /// <inheritdoc />
    public async Task<WorkflowState> CancelWorkflowAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        await DemandUnownedAsync(workflowState.Id, cancellationToken);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, workflowState, cancellationToken: cancellationToken);
        using var executionScope = WorkflowExecutionScope.Begin(workflowExecutionContext);
        await CancelWorkflowAsync(workflowExecutionContext, cancellationToken);
        return workflowStateExtractor.Extract(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task CancelWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var guard = serviceProvider.GetService(typeof(IWorkflowExecutionGuard)) as IWorkflowExecutionGuard ?? workflowExecutionContext.GetService<IWorkflowExecutionGuard>();
        if (guard != null)
        {
            // Exact-context denial precedes cancellation notifications, even if its mutable
            // ID was changed after preparation. This never consumes runner authority.
            await guard.AuthorizeAsync(workflowExecutionContext, WorkflowExecutionEntryPoint.DirectPipeline);
        }
        using var executionScope = WorkflowExecutionScope.Begin(workflowExecutionContext);
        await mediator.SendAsync(new WorkflowCancelling(workflowExecutionContext.Id), cancellationToken);
        var pipelineBuilder = new WorkflowExecutionPipelineBuilder(serviceProvider);
        workflowExecutionPipeline.ConfigurePipelineBuilder(pipelineBuilder);
        pipelineBuilder.ReplaceTerminal<CancelWorkflowMiddleware>();
        var pipeline = pipelineBuilder.Build();
        await pipeline(workflowExecutionContext);
        await mediator.SendAsync(new WorkflowCancelled(workflowExecutionContext.Id), cancellationToken);
    }

    private async ValueTask DemandUnownedAsync(string instanceId, CancellationToken cancellationToken)
    {
        if (serviceProvider.GetService(typeof(IWorkflowExecutionGuard)) is IWorkflowExecutionGuard guard)
        {
            await guard.DemandUnownedAsync(instanceId, cancellationToken);
        }
    }
}
