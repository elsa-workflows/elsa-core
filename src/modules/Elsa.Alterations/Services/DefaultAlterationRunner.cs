using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Results;
using Elsa.Alterations.Middleware.Workflows;
using Elsa.Common.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Services;

/// <inheritdoc />
public class DefaultAlterationRunner : IAlterationRunner
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowExecutionPipeline _workflowExecutionPipeline;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationRunner"/> class.
    /// </summary>
    public DefaultAlterationRunner(
        IWorkflowRuntime workflowRuntime,
        IWorkflowExecutionPipeline workflowExecutionPipeline,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateExtractor workflowStateExtractor,
        ISystemClock systemClock,
        IServiceProvider serviceProvider)
    {
        _workflowRuntime = workflowRuntime;
        _workflowExecutionPipeline = workflowExecutionPipeline;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowStateExtractor = workflowStateExtractor;
        _systemClock = systemClock;
        _serviceProvider = serviceProvider;
    }

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
        var log = new AlterationLog(_systemClock);
        var result = new RunAlterationsResult(workflowInstanceId, log);
        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        if (workflowState == null)
        {
            log.Add($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            return result;
        }

        var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(workflowState.DefinitionVersionId, cancellationToken);

        if (workflowGraph == null)
        {
            log.Add($"Workflow definition with ID '{workflowState.DefinitionVersionId}' not found.", LogLevel.Error);
            return result;
        }

        // Create workflow execution context.
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflowGraph, workflowState, cancellationTokens: cancellationToken);
        workflowExecutionContext.TransientProperties.Add(RunAlterationsMiddleware.AlterationsPropertyKey, alterations);
        workflowExecutionContext.TransientProperties.Add(RunAlterationsMiddleware.AlterationsLogPropertyKey, log);

        // Build a new workflow execution pipeline.
        var pipelineBuilder = new WorkflowExecutionPipelineBuilder(_serviceProvider);
        _workflowExecutionPipeline.ConfigurePipelineBuilder(pipelineBuilder);

        // Replace the terminal DefaultActivitySchedulerMiddleware with the RunAlterationsMiddleware terminal.
        pipelineBuilder.ReplaceTerminal<RunAlterationsMiddleware>();

        // Build modified pipeline.
        var pipeline = pipelineBuilder.Build();

        // Execute the pipeline.
        await pipeline(workflowExecutionContext);

        // Extract workflow state.
        workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);

        // Apply updated workflow state.
        await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);

        // Check if the workflow has scheduled work.
        result.WorkflowHasScheduledWork = workflowExecutionContext.Scheduler.HasAny;

        return result;
    }
}