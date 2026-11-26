using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// Deletes scheduled jobs based on deleted workflow triggers and bookmarks.
/// It looks up only the triggers whose name is in <see cref="RuntimeOptions.ScheduledTriggerNames"/>.
/// </summary>
public class DeleteSchedules(ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler, ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IOptions<RuntimeOptions> runtimeOptions)
    :
        INotificationHandler<WorkflowDefinitionDeleting>,
        INotificationHandler<WorkflowDefinitionsDeleting>,
        INotificationHandler<WorkflowDefinitionVersionDeleting>,
        INotificationHandler<WorkflowDefinitionVersionsDeleting>,
        INotificationHandler<BookmarksDeleting>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSchedules"/> class that looks up the triggers of the scheduling module.
    /// </summary>
    [Obsolete("Use the constructor that accepts IOptions<RuntimeOptions> instead. Removed in the next major version.")]
    public DeleteSchedules(ITriggerScheduler triggerScheduler, IBookmarkScheduler bookmarkScheduler, ITriggerStore triggerStore, IBookmarkStore bookmarkStore)
        : this(triggerScheduler, bookmarkScheduler, triggerStore, bookmarkStore, Microsoft.Extensions.Options.Options.Create(new RuntimeOptions { ScheduledTriggerNames = SchedulingStimulusNames.ScheduledTriggerNames.ToHashSet() }))
    {
    }

    async Task INotificationHandler<BookmarksDeleting>.HandleAsync(BookmarksDeleting notification, CancellationToken cancellationToken)
    {
        var ids = notification.Bookmarks.Select(x => x.Id).ToList();
        var bookmarks = await bookmarkStore.FindManyAsync(new BookmarkFilter { BookmarkIds = ids }, cancellationToken);
        await bookmarkScheduler.UnscheduleAsync(bookmarks, cancellationToken);
    }

    async Task INotificationHandler<WorkflowDefinitionDeleting>.HandleAsync(WorkflowDefinitionDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionId = notification.DefinitionId }, cancellationToken);
    async Task INotificationHandler<WorkflowDefinitionsDeleting>.HandleAsync(WorkflowDefinitionsDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionIds = notification.DefinitionIds }, cancellationToken);

    async Task INotificationHandler<WorkflowDefinitionVersionDeleting>.HandleAsync(WorkflowDefinitionVersionDeleting notification, CancellationToken cancellationToken) =>
        await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionVersionId = notification.WorkflowDefinition.Id }, cancellationToken);

    async Task INotificationHandler<WorkflowDefinitionVersionsDeleting>.HandleAsync(WorkflowDefinitionVersionsDeleting notification, CancellationToken cancellationToken) => await RemoveSchedulesAsync(new TriggerFilter { WorkflowDefinitionVersionIds = notification.Ids }, cancellationToken);

    private async Task RemoveSchedulesAsync(TriggerFilter filter, CancellationToken cancellationToken)
    {
        var scheduledTriggerNames = runtimeOptions.Value.ScheduledTriggerNames;

        if (scheduledTriggerNames.Count == 0)
            return;

        filter.Names = scheduledTriggerNames.ToList();
        var triggers = await triggerStore.FindManyAsync(filter, cancellationToken);
        await triggerScheduler.UnscheduleAsync(triggers, cancellationToken);
    }
}