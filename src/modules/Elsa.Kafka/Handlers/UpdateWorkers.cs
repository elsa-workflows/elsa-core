using Elsa.Extensions;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Kafka.Handlers;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateWorkers(IWorkerManager workerManager, IWorkflowDefinitionService workflowDefinitionService) : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksIndexed>
{
    /// Adds, updates and removes workers based on added and removed triggers.
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var removedTriggers = notification.IndexedWorkflowTriggers.RemovedTriggers;
        var removedTriggerIds = removedTriggers.Select(x => x.Id).ToList();
        
        foreach (var removedTrigger in removedTriggers)
        {
            var consumerDefinitionId = removedTrigger.GetPayload<MessageReceivedStimulus>().ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);
            worker.RemoveTriggers(removedTriggerIds);
        }
        
        var addedTriggers = notification.IndexedWorkflowTriggers.AddedTriggers;
        
        foreach (var addedTrigger in addedTriggers)
        {
            var stimulus = addedTrigger.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);
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
        var removedBookmarks = notification.IndexedWorkflowBookmarks.RemovedBookmarks;
        var removedBookmarkIds = removedBookmarks.Select(x => x.Id).ToList();
        
        foreach (var removedBookmark in removedBookmarks)
        {
            var consumerDefinitionId = removedBookmark.GetPayload<MessageReceivedStimulus>().ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);
            worker.RemoveBookmarks(removedBookmarkIds);
        }
        
        var addedBookmarks = notification.IndexedWorkflowBookmarks.AddedBookmarks;
        var workflowInstanceId = notification.IndexedWorkflowBookmarks.WorkflowExecutionContext.Id;
        
        foreach (var addedBookmark in addedBookmarks)
        {
            var stimulus = addedBookmark.GetPayload<MessageReceivedStimulus>();
            var consumerDefinitionId = stimulus.ConsumerDefinitionId;
            var worker = workerManager.GetWorker(consumerDefinitionId);
            var bookmarkBinding = new BookmarkBinding(workflowInstanceId, addedBookmark.Id, stimulus);
            worker.BindBookmark(bookmarkBinding);
        }

        return Task.CompletedTask;
    }
}