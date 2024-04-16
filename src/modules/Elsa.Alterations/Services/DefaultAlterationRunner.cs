using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Results;
using Elsa.Alterations.Middleware.Workflows;
using Elsa.Common.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
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

        // Load workflow instance.
        var workflowState = await workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        // If the workflow instance is not found, log an error and continue.
        if (workflowState == null)
        {
            log.Add($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            return result;
        }

        // Load workflow definition.
        var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowState.DefinitionVersionId, cancellationToken);

        // If the workflow definition is not found, log an error and continue.
        if (workflow == null)
        {
            log.Add($"Workflow definition with ID '{workflowState.DefinitionVersionId}' not found.", LogLevel.Error);
            return result;
        }
        
        // Create workflow execution context.
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(serviceProvider, workflow, workflowState, cancellationToken: cancellationToken);
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
        await workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);

        // Check if the workflow has scheduled work.
        result.WorkflowHasScheduledWork = workflowExecutionContext.Scheduler.HasAny;

        return result;
    }
}