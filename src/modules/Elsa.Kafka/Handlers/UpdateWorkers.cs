using Elsa.Extensions;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Kafka.Handlers;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateWorkers(IWorkerManager workerManager, IWorkflowDefinitionService workflowDefinitionService) :
    INotificationHandler<WorkflowTriggersIndexed>,
    INotificationHandler<WorkflowBookmarksIndexed>,
    INotificationHandler<BookmarksDeleted>
{
    private static readonly string MessageReceivedActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();

    /// Adds, updates and removes workers based on added and removed triggers.
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var removedTriggers = notification.IndexedWorkflowTriggers.RemovedTriggers.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();
        var removedTriggerIds = removedTriggers.Select(x => x.Id).ToList();

        foreach (var removedTrigger in removedTriggers)
        {
            var consumerDefinitionId = removedTrigger.GetPayload<MessageReceivedStimulus>().ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);

            worker?.RemoveTriggers(removedTriggerIds);
        }

        var addedTriggers = notification.IndexedWorkflowTriggers.AddedTriggers.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();

        foreach (var addedTrigger in addedTriggers)
        {
            var stimulus = addedTrigger.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);

            if (worker == null)
                continue;

            var workflow = await workflowDefinitionService.FindWorkflowAsync(addedTrigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflow == null)
                continue;

            var triggerBinding = new TriggerBinding(workflow, addedTrigger.Id, addedTrigger.ActivityId, stimulus);
            worker.BindTrigger(triggerBinding);
        }
    }

    /// Adds, updates and removes workers based on added and removed bookmarks.
    public Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        RemoveBookmarkBindings(notification.IndexedWorkflowBookmarks.RemovedBookmarks.Where(x => x.Name == MessageReceivedActivityTypeName).ToList());

        var addedBookmarks = notification.IndexedWorkflowBookmarks.AddedBookmarks.Where(x => x.Name == MessageReceivedActivityTypeName).ToList();
        var workflowInstanceId = notification.IndexedWorkflowBookmarks.WorkflowExecutionContext.Id;
        var correlationId = notification.IndexedWorkflowBookmarks.WorkflowExecutionContext.CorrelationId;

        foreach (var addedBookmark in addedBookmarks)
        {
            var stimulus = addedBookmark.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);

            if (worker == null)
                continue;

            var bookmarkBinding = new BookmarkBinding(workflowInstanceId, correlationId, addedBookmark.Id, stimulus);
            worker.BindBookmark(bookmarkBinding);
        }

        return Task.CompletedTask;
    }

    public Task HandleAsync(BookmarksDeleted notification, CancellationToken cancellationToken)
    {
        RemoveBookmarkBindings(notification.Bookmarks.Where(x => x.ActivityTypeName == MessageReceivedActivityTypeName).Select(x => x.ToBookmark()).ToList());
        return Task.CompletedTask;
    }

    private void RemoveBookmarkBindings(ICollection<Bookmark> bookmarks)
    {
        var removedBookmarkIds = bookmarks.Select(x => x.Id).ToList();
        foreach (var bookmark in bookmarks)
        {
            var stimulus = bookmark.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);

            worker?.RemoveBookmarks(removedBookmarkIds);
        }
    }
}