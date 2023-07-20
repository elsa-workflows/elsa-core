using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceManager : IWorkflowInstanceManager
{
    private readonly IWorkflowInstanceStore _store;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstanceManager"/> class.
    /// </summary>
    public WorkflowInstanceManager(IWorkflowInstanceStore store, INotificationSender notificationSender)
    {
        _store = store;
        _notificationSender = notificationSender;
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
}