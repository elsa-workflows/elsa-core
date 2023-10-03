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
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
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
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IWorkflowStateExtractor workflowStateExtractor,
        ISystemClock systemClock,
        IServiceProvider serviceProvider)
    {
        _handlers = handlers;
        _workflowRuntime = workflowRuntime;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
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
        var workflowExecutionContext = await _workflowExecutionContextFactory.CreateAsync(_serviceProvider, workflow, workflowState.Id, workflowState, cancellationTokens: cancellationToken);

        // Execute alterations.
        await RunAsync(workflowExecutionContext, alterations, log, cancellationToken);
        
        // Update workflow state.
        workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);

        // Apply updated workflow state.
        await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);

        return result;
    }

    private async Task RunAsync(WorkflowExecutionContext workflowExecutionContext, IEnumerable<IAlteration> alterations, AlterationLog log, CancellationToken cancellationToken = default)
    {
        foreach (var alteration in alterations)
        {
            // Find handler.
            var handler = _handlers.FirstOrDefault(x => x.CanHandle(alteration));

            // If no handler is found, log an error and continue.
            if (handler == null)
            {
                log.Add($"No handler found for alteration '{alteration.GetType().Name}'.", LogLevel.Error);
                continue;
            }

            // Execute handler.
            var alterationHandlerContext = new AlterationHandlerContext(alteration, workflowExecutionContext, log, _serviceProvider, cancellationToken);
            await handler.HandleAsync(alterationHandlerContext);

            // If the handler has failed, mark the result as failed.
            if (alterationHandlerContext.HasFailed)
                break;
        }
    }
}