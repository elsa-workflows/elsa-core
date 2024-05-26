using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Results;
using Elsa.Alterations.Middleware.Workflows;
using Elsa.Common.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Services;

/// <inheritdoc />
public class DefaultAlterationRunner(
    IWorkflowRuntime workflowRuntime,
    IWorkflowExecutionPipeline workflowExecutionPipeline,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowStateExtractor workflowStateExtractor,
    ISystemClock systemClock,
    IServiceProvider serviceProvider)
    : IAlterationRunner
{
    /// <inheritdoc />
    public async Task<ICollection<RunAlterationsResult>> RunAsync(IEnumerable<string> workflowInstanceIds, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default)
    {
        var results = new List<RunAlterationsResult>();
        var alterationList = alterations as ICollection<IAlteration> ?? alterations.ToList();

        foreach (var workflowInstanceId in workflowInstanceIds)
        {
            var result = await RunAsync(workflowInstanceId, alterationList, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<RunAlterationsResult> RunAsync(string workflowInstanceId, IEnumerable<IAlteration> alterations, CancellationToken cancellationToken = default)
    {
        var log = new AlterationLog(systemClock);
        var result = new RunAlterationsResult(workflowInstanceId, log);
        var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken: cancellationToken);

        // Load workflow instance.
        var workflowState = await workflowClient.ExportStateAsync(cancellationToken);

        if (workflowState == null)
        {
            log.Add($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            return result;
        }

        // Load workflow definition.
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowState.DefinitionVersionId, cancellationToken);

        if (workflowGraph == null)
        {
            log.Add($"Workflow definition with ID '{workflowState.DefinitionVersionId}' not found.", LogLevel.Error);
            return result;
        }

        // Create workflow execution context.
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowGraph, workflowState, cancellationToken: cancellationToken);
        workflowExecutionContext.TransientProperties.Add(RunAlterationsMiddleware.AlterationsPropertyKey, alterations);
        workflowExecutionContext.TransientProperties.Add(RunAlterationsMiddleware.AlterationsLogPropertyKey, log);

        // Build a new workflow execution pipeline.
        var pipelineBuilder = new WorkflowExecutionPipelineBuilder(serviceProvider);
        workflowExecutionPipeline.ConfigurePipelineBuilder(pipelineBuilder);

        // Replace the terminal DefaultActivitySchedulerMiddleware with the RunAlterationsMiddleware terminal.
        pipelineBuilder.ReplaceTerminal<RunAlterationsMiddleware>();

        // Build modified pipeline.
        var pipeline = pipelineBuilder.Build();

        // Execute the pipeline.
        await pipeline(workflowExecutionContext);

        // Extract workflow state.
        workflowState = workflowStateExtractor.Extract(workflowExecutionContext);

        // Apply updated workflow state.
        await workflowClient.ImportStateAsync(workflowState, cancellationToken);

        // Check if the workflow has scheduled work.
        result.WorkflowHasScheduledWork = workflowExecutionContext.Scheduler.HasAny;

        return result;
    }
}