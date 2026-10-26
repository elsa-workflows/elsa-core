using Elsa.Common;
using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowCancellationService(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowCancellationDispatcher dispatcher,
    ISystemClock systemClock,
    ILogger<WorkflowCancellationService> logger,
    IWorkflowExecutionGuard? workflowExecutionGuard = null)
    : IWorkflowCancellationService
{
    /// <inheritdoc />
    public async Task<bool> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = workflowInstanceId
        };
        var instance = await workflowInstanceStore.FindAsync(filter, cancellationToken);

        if(instance == null)
            return false;
            
        await CancelWorkflows([instance], cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowsAsync(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            Ids = workflowInstanceIds.ToList()
        };
        var instances = await workflowInstanceStore.FindManyAsync(filter, cancellationToken);
        return await CancelWorkflows(instances.ToList(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowByDefinitionVersionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionVersionId = definitionVersionId,
            WorkflowStatus = WorkflowStatus.Running
        };
        var instances = (await workflowInstanceStore.FindManyAsync(filter, cancellationToken)).ToList();
        var instanceIds = instances.Select(i => i.Id).ToList();

        return await CancelWorkflowsAsync(instanceIds, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CancelWorkflowByDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        // Shouldn't we get possible multiple definitions here?
        var workflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
        if (workflowDefinition is null)
            return 0;

        return await CancelWorkflowByDefinitionVersionAsync(workflowDefinition.Id, cancellationToken);
    }

    private async Task<int> CancelWorkflows(IList<WorkflowInstance> workflowInstances, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var instance in workflowInstances)
        {
            if (instance.Status == WorkflowStatus.Finished)
                continue;

            tasks.Add(CancelInstanceAsync(instance, cancellationToken));
        }

        var instanceIds = workflowInstances.Select(i => i.Id).ToList();
        await CancelChildWorkflowInstances(instanceIds, cancellationToken);
        await Task.WhenAll(tasks);

        return tasks.Count;
    }

    /// <summary>
    /// Marks a non-executing, unowned instance as Cancelling, then dispatches.
    /// A crash after the mark and before enqueue is not recovered here.
    /// </summary>
    private async Task CancelInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        if (!await CanPersistCancellingAsync(instance, cancellationToken))
        {
            await DispatchCancellationAsync(instance, cancellationToken);
            return;
        }

        var previousSubStatus = instance.SubStatus;
        var cancellingAt = TruncateToMilliseconds(systemClock.UtcNow);
        var marked = await workflowInstanceStore.TryMarkCancellingAsync(instance.Id, previousSubStatus, cancellingAt, cancellationToken);

        if (!marked)
        {
            // The conditional update declined or lost a race. Never replace that with a full-instance save.
            logger.LogDebug(
                "Workflow instance {WorkflowInstanceId} was not marked {CancellingSubStatus}. Dispatching cancellation without an interim sub-status.",
                instance.Id,
                WorkflowSubStatus.Cancelling);
            await DispatchCancellationAsync(instance, cancellationToken);
            return;
        }

        try
        {
            await DispatchMarkedCancellationAsync(instance, previousSubStatus, cancellingAt, cancellationToken);
        }
        catch (Exception exception)
        {
            await RestoreAfterDispatchFailureAsync(instance.Id, previousSubStatus, cancellingAt, exception);
            throw;
        }
    }

    /// <summary>
    /// Executing instances keep their sub-status. Admission ownership is the guard's <c>DemandUnownedAsync</c>;
    /// when that guard is not registered there is no durable owner to check. An unclear answer fails closed and skips the mark.
    /// </summary>
    private async ValueTask<bool> CanPersistCancellingAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        if (instance.Status != WorkflowStatus.Running
            || instance.IsExecuting
            || !IsCancellableSubStatus(instance.SubStatus))
        {
            logger.LogDebug(
                "Workflow instance {WorkflowInstanceId} is executing or not a running cancellable sub-status. Leaving {SubStatus} unchanged.",
                instance.Id,
                instance.SubStatus);
            return false;
        }

        if (workflowExecutionGuard is null)
            return true;

        try
        {
            await workflowExecutionGuard.DemandUnownedAsync(instance.Id, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogDebug(
                exception,
                "Workflow instance {WorkflowInstanceId} is owned, or ownership could not be established. Leaving {SubStatus} unchanged.",
                instance.Id,
                instance.SubStatus);
            return false;
        }
    }

    private async Task<bool> DispatchCancellationAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        await dispatcher.DispatchAsync(new DispatchCancelWorkflowRequest
        {
            WorkflowInstanceId = instance.Id
        }, cancellationToken);

        return true;
    }

    private async Task DispatchMarkedCancellationAsync(
        WorkflowInstance instance,
        WorkflowSubStatus previousSubStatus,
        DateTimeOffset cancellingAt,
        CancellationToken cancellationToken)
    {
        await dispatcher.DispatchAsync(new DispatchCancelWorkflowRequest
        {
            WorkflowInstanceId = instance.Id,
            PreviousSubStatus = previousSubStatus,
            CancellingAt = cancellingAt
        }, cancellationToken);
    }

    private async Task RestoreAfterDispatchFailureAsync(
        string workflowInstanceId,
        WorkflowSubStatus previousSubStatus,
        DateTimeOffset cancellingAt,
        Exception dispatchFailure)
    {
        try
        {
            // The caller's token may already be cancelled. The rollback still has to run.
            var restored = await workflowInstanceStore.TryRestoreAsync(
                workflowInstanceId,
                previousSubStatus,
                cancellingAt,
                TruncateToMilliseconds(systemClock.UtcNow),
                CancellationToken.None);

            if (!restored)
            {
                logger.LogDebug(
                    dispatchFailure,
                    "Cancellation dispatch failed for workflow instance {WorkflowInstanceId}, and the Cancelling mark was no longer current.",
                    workflowInstanceId);
            }
        }
        catch (Exception restoreFailure)
        {
            logger.LogError(
                restoreFailure,
                "Failed to restore workflow instance {WorkflowInstanceId} after cancellation dispatch failed.",
                workflowInstanceId);
        }
    }

    private async Task CancelChildWorkflowInstances(IEnumerable<string> workflowInstanceIds, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<int>>();
        var workflowInstanceIdBatches = workflowInstanceIds.Chunk(50);

        foreach (var workflowInstanceIdBatch in workflowInstanceIdBatches)
        {
            // Find child instances for the current workflow instance ID and cancel them.
            // Do not check on status as their children might still be running and need to be canceled.
            WorkflowInstanceFilter filter = new()
            {
                ParentWorkflowInstanceIds = workflowInstanceIdBatch.ToList()
            };
            var childInstances = (await workflowInstanceStore.FindManyAsync(filter, cancellationToken)).ToList();

            if (childInstances.Any())
                tasks.Add(CancelWorkflows(childInstances, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Millisecond stamps survive providers that round timestamps to microseconds.
    /// </summary>
    private static DateTimeOffset TruncateToMilliseconds(DateTimeOffset value)
    {
        var ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTimeOffset(ticks, value.Offset);
    }

    private static bool IsCancellableSubStatus(WorkflowSubStatus subStatus) =>
        subStatus is WorkflowSubStatus.Pending or WorkflowSubStatus.Suspended or WorkflowSubStatus.Interrupted;
}
