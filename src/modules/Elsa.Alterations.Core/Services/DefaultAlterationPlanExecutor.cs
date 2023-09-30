using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Results;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationPlanExecutor : IAlterationPlanExecutor
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
    private readonly IEnumerable<IAlterationHandler> _handlers;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanExecutor"/> class.
    /// </summary>
    public DefaultAlterationPlanExecutor(
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IEnumerable<IAlterationHandler> handlers,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock,
        IServiceProvider serviceProvider)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _handlers = handlers;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<AlterationPlanExecutionResult> ExecuteAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
    {
        var workflowInstanceIds = plan.WorkflowInstanceIds;
        var alterationLog = new DefaultAlterationLog(_systemClock);
        var planResult = new AlterationPlanExecutionResult(alterationLog);

        // Apply alterations to workflow instances.
        foreach (var workflowInstanceId in workflowInstanceIds)
        {
            var result = await ProcessWorkflowInstanceAsync(workflowInstanceId, plan, cancellationToken);
            plan.ProcessedWorkflowInstanceIds.Add(workflowInstanceId);

            if (!result.HasSucceeded)
            {
                result.HasSucceeded = false;
                planResult.HasSucceeded = false;
            }

            planResult.Log.AddRange(result.Log.LogEntries);

            if (result.ModifiedWorkflowExecutionContext != null)
                planResult.ModifiedWorkflowExecutionContexts.Add(result.ModifiedWorkflowExecutionContext);
        }

        return planResult;
    }

    private async Task<AlterationExecutionResult> ProcessWorkflowInstanceAsync(string workflowInstanceId, AlterationPlan plan, CancellationToken cancellationToken)
    {
        // Load workflow instance.
        var workflowInstance = await _workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = workflowInstanceId }, cancellationToken);
        var log = new DefaultAlterationLog(_systemClock);

        // If the workflow instance is not found, log an error and continue.
        if (workflowInstance == null)
        {
            log.Append($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            return new AlterationExecutionResult(log, false);
        }

        // Load workflow definition.
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowInstance.DefinitionVersionId, cancellationToken);

        // If the workflow definition is not found, log an error and continue.
        if (workflowDefinition == null)
        {
            log.Append($"Workflow definition with ID '{workflowInstance.DefinitionVersionId}' not found.", LogLevel.Error);
            return new AlterationExecutionResult(log, false);
        }

        // Materialize workflow.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);

        // Create workflow execution context.
        var workflowExecutionContext = await _workflowExecutionContextFactory.CreateAsync(_serviceProvider, workflow, workflowInstance.Id, workflowInstance.WorkflowState, cancellationTokens: cancellationToken);

        // Execute alterations.
        foreach (var alteration in plan.Alterations)
        {
            // Find handler.
            var handler = _handlers.FirstOrDefault(x => x.CanHandle(alteration));

            // If no handler is found, log an error and continue.
            if (handler == null)
            {
                log.Append($"No handler found for alteration '{alteration.GetType().Name}'.", LogLevel.Error);
                continue;
            }

            // Execute handler.
            var alterationHandlerContext = new AlterationHandlerContext(plan, alteration, workflowExecutionContext, log, _serviceProvider, cancellationToken);
            await handler.HandleAsync(alterationHandlerContext);

            // If the handler has failed, mark the result as failed.
            if (alterationHandlerContext.HasFailed)
            {
                return new AlterationExecutionResult(log, false);
            }
        }

        return new AlterationExecutionResult(log, true)
        {
            ModifiedWorkflowExecutionContext = workflowExecutionContext
        };
    }
}