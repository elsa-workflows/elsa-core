using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCanceler(IWorkflowExecutionPipeline workflowExecutionPipeline, IWorkflowStateExtractor workflowStateExtractor, IServiceProvider serviceProvider) : IWorkflowCanceler
{
    /// <inheritdoc />
    public async Task<WorkflowState> CancelWorkflowAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, workflowState, cancellationToken: cancellationToken);

        // Alter the workflow execution context to cancel the workflow.
        await CancelWorkflowAsync(workflowExecutionContext, cancellationToken);
        
        // Map the workflow execution context back to a workflow state.
        return workflowStateExtractor.Extract(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task CancelWorkflowAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        // Build a new workflow execution pipeline.
        var pipelineBuilder = new WorkflowExecutionPipelineBuilder(serviceProvider);
        workflowExecutionPipeline.ConfigurePipelineBuilder(pipelineBuilder);

        // Replace the terminal DefaultActivitySchedulerMiddleware with the CancelWorkflowMiddleware terminal.
        pipelineBuilder.ReplaceTerminal<CancelWorkflowMiddleware>();

        // Build modified pipeline.
        var pipeline = pipelineBuilder.Build();

        // Execute the pipeline.
        await pipeline(workflowExecutionContext);
    }
}