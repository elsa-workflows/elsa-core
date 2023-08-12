using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// Deletes scheduled jobs based on deleted workflow triggers and bookmarks.
/// </summary>
public class DeleteSchedules :
    INotificationHandler<WorkflowDefinitionDeleting>,
    INotificationHandler<WorkflowDefinitionsDeleting>,
    INotificationHandler<WorkflowDefinitionVersionDeleting>,
    INotificationHandler<WorkflowDefinitionVersionsDeleting>,
    INotificationHandler<BookmarksDeleting>
{
    private readonly ITriggerScheduler _triggerScheduler;
    private readonly IBookmarkScheduler _bookmarkScheduler;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleWorkflows"/> class.
    /// </summary>
    public DeleteSchedules(ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler, ITriggerStore triggerStore, IBookmarkStore bookmarkStore)
    {
        _triggerScheduler = triggerScheduler;
        _bookmarkScheduler = bookmarkScheduler;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    async Task INotificationHandler<BookmarksDeleting>.HandleAsync(BookmarksDeleting notification, CancellationToken cancellationToken)
    {
        var ids = notification.Bookmarks.Select(x => x.BookmarkId).ToList();
        var bookmarks = await _bookmarkStore.FindManyAsync(new BookmarkFilter { BookmarkIds = ids }, cancellationToken);
        await _bookmarkScheduler.UnscheduleAsync(bookmarks, cancellationToken);
    }

    async Task INotificationHandler<WorkflowDefinitionDeleting>.HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionId = notification.DefinitionId }, cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionsDeleting>.HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionIds = notification.DefinitionIds }, cancellationToken);

    async Task INotificationHandler<WorkflowDefinitionVersionDeleting>.HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken) =>
        await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionVersionId = notification.WorkflowDefinition.Id }, cancellationToken);

    async Task INotificationHandler<WorkflowDefinitionVersionsDeleting>.HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionVersionIds = notification.Ids }, cancellationToken);

    private async Task RemoveSchedulesAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        var triggers = await _triggerStore.FindManyAsync(filter, cancellationToken);
        await _triggerScheduler.UnscheduleAsync(triggers, cancellationToken);
    }
}