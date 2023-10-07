using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Results;
using Elsa.Common.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationRunner : IAlterationRunner
{
    private readonly IEnumerable<IAlterationHandler> _handlers;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationRunner"/> class.
    /// </summary>
    public DefaultAlterationRunner(
        IEnumerable<IAlterationHandler> handlers,
        IWorkflowRuntime workflowRuntime,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowStateExtractor workflowStateExtractor,
        ISystemClock systemClock,
        IServiceProvider serviceProvider)
    {
        _handlers = handlers;
        _workflowRuntime = workflowRuntime;
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

        // Load workflow instance.
        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        // If the workflow instance is not found, log an error and continue.
        if (workflowState == null)
        {
            log.Add($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            return result;
        }

        // Load workflow definition.
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowState.DefinitionVersionId, cancellationToken);

        // If the workflow definition is not found, log an error and continue.
        if (workflowDefinition == null)
        {
            log.Add($"Workflow definition with ID '{workflowState.DefinitionVersionId}' not found.", LogLevel.Error);
            return result;
        }

        // Materialize workflow.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);

        // Create workflow execution context.
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflow, workflowState, cancellationTokens: cancellationToken);

        // Execute alterations.
        var success = await RunAsync(workflowExecutionContext, alterations, log, cancellationToken);
        
        // If the alterations have failed, exit.
        if (!success)
            return result;

        // Update workflow state.
        workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);

        // Apply updated workflow state.
        await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);

        // Check if the workflow has scheduled work.
        result.WorkflowHasScheduledWork = workflowExecutionContext.Scheduler.HasAny;

        return result;
    }

    private async Task<bool> RunAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<IAlteration> alterations, AlterationLog log, CancellationToken cancellationToken = default)
    {
        var commitActions = new List<Func<Task>>();

        foreach (var alteration in alterations)
        {
            // Find handlers.
            var handlers = _handlers.Where(x => x.CanHandle(alteration)).ToList();

            foreach (var handler in handlers)
            {
                // Execute handler.
                var alterationContext = new AlterationContext(alteration, workflowExecutionContext, log, cancellationToken);
                await handler.HandleAsync(alterationContext);

                // If the handler has failed, exit.
                if (alterationContext.HasFailed)
                    return false;

                // Collect the commit handler, if any.
                if (alterationContext.CommitAction != null)
                    commitActions.Add(alterationContext.CommitAction);
            }
        }

        // Execute commit handlers.
        foreach (var commitAction in commitActions)
            await commitAction();
        
        return true;
    }
}