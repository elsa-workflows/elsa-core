using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.AzureServiceBus.Handlers;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateWorkers(IWorkerManager workerManager) : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksIndexed>
{
    /// Adds, updates and removes workers based on added and removed triggers.
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var added = notification.IndexedWorkflowTriggers.AddedTriggers.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedStimulus>());
        await StartWorkersAsync(added, cancellationToken);
    }

    /// Adds, updates and removes workers based on added and removed bookmarks.
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var added = notification.IndexedWorkflowBookmarks.AddedBookmarks.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedStimulus>());
        await StartWorkersAsync(added, cancellationToken);
    }

    private async Task StartWorkersAsync(IEnumerable<MessageReceivedStimulus> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await workerManager.StartWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }
}