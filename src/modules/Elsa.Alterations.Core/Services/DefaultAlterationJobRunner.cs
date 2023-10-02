using System.Text.Json;
using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationJobRunner : IAlterationJobRunner
{
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowInstanceManager _workflowInstanceManager;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
    private readonly IEnumerable<IAlterationHandler> _handlers;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanRunner"/> class.
    /// </summary>
    public DefaultAlterationJobRunner(
        IAlterationPlanStore alterationPlanStore,
        IAlterationJobStore alterationJobStore,
        IWorkflowRuntime workflowRuntime,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowInstanceManager workflowInstanceManager,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IEnumerable<IAlterationHandler> handlers,
        INotificationSender notificationSender,
        ISystemClock systemClock,
        IServiceProvider serviceProvider)
    {
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _workflowRuntime = workflowRuntime;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowInstanceManager = workflowInstanceManager;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _handlers = handlers;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = (await _alterationJobStore.FindAsync(new AlterationJobFilter { Id = jobId }, cancellationToken))!;
        var plan = (await _alterationPlanStore.FindAsync(new AlterationPlanFilter { Id = job.PlanId }, cancellationToken))!;
        var log = new AlterationLog(_systemClock);

        await ProcessWorkflowInstanceAsync(plan, job, log, cancellationToken);
        await SaveJobAsync(job, log, cancellationToken);

        // Publish a notification.
        await _notificationSender.SendAsync(new AlterationJobCompleted(job), cancellationToken);

        return job;
    }

    private async Task ProcessWorkflowInstanceAsync(AlterationPlan plan, AlterationJob job, AlterationLog log, CancellationToken cancellationToken)
    {
        // Load workflow instance.
        var workflowInstanceId = job.WorkflowInstanceId;
        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        // If the workflow instance is not found, log an error and continue.
        if (workflowState == null)
        {
            log.Append($"Workflow instance with ID '{workflowInstanceId}' not found.", LogLevel.Error);
            job.Status = AlterationJobStatus.Failed;
            return;
        }

        // Load workflow definition.
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowState.DefinitionVersionId, cancellationToken);

        // If the workflow definition is not found, log an error and continue.
        if (workflowDefinition == null)
        {
            log.Append($"Workflow definition with ID '{workflowState.DefinitionVersionId}' not found.", LogLevel.Error);
            return;
        }

        // Materialize workflow.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);

        // Create workflow execution context.
        var workflowExecutionContext = await _workflowExecutionContextFactory.CreateAsync(_serviceProvider, workflow, workflowState.Id, workflowState, cancellationTokens: cancellationToken);

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
                return;
        }

        // Apply updated workflow state.
        await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);

        // Update job status.
        workflowState = _workflowInstanceManager.ExtractWorkflowState(workflowExecutionContext);
        job.Status = AlterationJobStatus.Completed;
        job.SerializedWorkflowState = await _workflowInstanceManager.SerializeWorkflowStateAsync(workflowState, cancellationToken);
        job.CompletedAt = _systemClock.UtcNow;
    }

    private async Task SaveJobAsync(AlterationJob job, AlterationLog log, CancellationToken cancellationToken)
    {
        job.SerializedLog = JsonSerializer.Serialize(log.LogEntries);
        await _alterationJobStore.SaveAsync(job, cancellationToken);
    }
}