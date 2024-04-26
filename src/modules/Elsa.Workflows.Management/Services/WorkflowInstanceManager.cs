using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Params;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.State;
using Exception = System.Exception;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceManager(
    IWorkflowInstanceStore store,
    IWorkflowInstanceFactory workflowInstanceFactory, 
    INotificationSender notificationSender,
    WorkflowStateMapper workflowStateMapper,
    IWorkflowStateExtractor workflowStateExtractor,
    IWorkflowStateSerializer workflowStateSerializer)
    : IWorkflowInstanceManager
{
    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindByIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(workflowInstance, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> SaveAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await SaveAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> SaveAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var workflowState = ExtractWorkflowState(workflowExecutionContext);
        return await SaveAsync(workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var instance = await store.FindAsync(filter, cancellationToken);

        if (instance == null)
            return false;

        var ids = new[]
        {
            instance.Id
        };
        await notificationSender.SendAsync(new WorkflowInstancesDeleting(ids), cancellationToken);
        await store.DeleteAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstancesDeleted(ids), cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var summaries = (await store.SummarizeManyAsync(filter, cancellationToken)).ToList();
        var ids = summaries.Select(x => x.Id).ToList();
        await notificationSender.SendAsync(new WorkflowInstancesDeleting(ids), cancellationToken);
        var count = await store.DeleteAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new WorkflowInstancesDeleted(ids), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public WorkflowState ExtractWorkflowState(WorkflowExecutionContext workflowExecutionContext)
    {
        return workflowStateExtractor.Extract(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<string> SerializeWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return await workflowStateSerializer.SerializeAsync(workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateWorkflowInstanceAsync(CreateWorkflowInstanceParams @params, CancellationToken cancellationToken = default)
    {
        var workflowInstance = workflowInstanceFactory.CreateWorkflowInstance(@params);
        await SaveAsync(workflowInstance, cancellationToken);
        return workflowInstance;
    }
}