using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultActivityExecutionManager : IActivityExecutionManager
{
    private readonly IActivityExecutionStore _store;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityExecutionManager"/> class.
    /// </summary>
    public DefaultActivityExecutionManager(IActivityExecutionStore store, INotificationSender notificationSender)
    {
        _store = store;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = (await _store.FindManyAsync(filter, cancellationToken)).ToList();

        foreach (var record in records)
        {
            await _store.DeleteManyAsync(filter, cancellationToken);
            await _notificationSender.SendAsync(new ActivityExecutionRecordDeleted(record), cancellationToken);
        }

        return records.Count;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(record, cancellationToken);
        await _notificationSender.SendAsync(new ActivityExecutionRecordUpdated(record), cancellationToken);
    }
}