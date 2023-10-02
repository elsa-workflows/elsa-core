using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceManager : IWorkflowInstanceManager
{
    private readonly IWorkflowInstanceStore _store;
    private readonly INotificationSender _notificationSender;
    private readonly WorkflowStateMapper _workflowStateMapper;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstanceManager"/> class.
    /// </summary>
    public WorkflowInstanceManager(
        IWorkflowInstanceStore store, 
        INotificationSender notificationSender, 
        WorkflowStateMapper workflowStateMapper, 
        IWorkflowStateExtractor workflowStateExtractor,
        IWorkflowStateSerializer workflowStateSerializer)
    {
        _store = store;
        _notificationSender = notificationSender;
        _workflowStateMapper = workflowStateMapper;
        _workflowStateExtractor = workflowStateExtractor;
        _workflowStateSerializer = workflowStateSerializer;
    }
    
    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(workflowInstance, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> SaveAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var workflowInstance = _workflowStateMapper.Map(workflowState)!;
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
        var instance = await _store.FindAsync(filter, cancellationToken);
        
        if(instance == null)
            return false;

        var ids = new[] { instance.Id };
        await _notificationSender.SendAsync(new WorkflowInstancesDeleting(ids), cancellationToken);
        await _store.DeleteAsync(filter, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowInstancesDeleted(ids), cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var summaries = (await _store.SummarizeManyAsync(filter, cancellationToken)).ToList();
        var ids = summaries.Select(x => x.Id).ToList();
        await _notificationSender.SendAsync(new WorkflowInstancesDeleting(ids), cancellationToken);
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowInstancesDeleted(ids), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public WorkflowState ExtractWorkflowState(WorkflowExecutionContext workflowExecutionContext)
    {
        return _workflowStateExtractor.Extract(workflowExecutionContext);
    }

    /// <inheritdoc />
    public async Task<string> SerializeWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return await _workflowStateSerializer.SerializeAsync(workflowState, cancellationToken);
    }
}