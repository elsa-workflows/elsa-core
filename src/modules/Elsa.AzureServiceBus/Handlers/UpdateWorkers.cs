using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.AzureServiceBus.Handlers;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
public class UpdateWorkers : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksIndexed>
{
    private readonly IWorkerManager _workerManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public UpdateWorkers(IWorkerManager workerManager)
    {
        _workerManager = workerManager;
    }

    /// <summary>
    /// Adds, updates and removes workers based on added and removed triggers.
    /// </summary>
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var added = notification.IndexedWorkflowTriggers.AddedTriggers.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());
        var removed = notification.IndexedWorkflowTriggers.RemovedTriggers.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());
        var unchanged = notification.IndexedWorkflowTriggers.UnchangedTriggers.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());

        await StopWorkersAsync(removed, cancellationToken);
        await StartWorkersAsync(added, cancellationToken);
        await EnsureWorkersAsync(unchanged, cancellationToken);
    }
    
    /// <summary>
    /// Adds, updates and removes workers based on added and removed bookmarks.
    /// </summary>
    public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
    {
        var added = notification.IndexedWorkflowBookmarks.AddedBookmarks.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());
        var removed = notification.IndexedWorkflowBookmarks.RemovedBookmarks.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());
        var unchanged = notification.IndexedWorkflowBookmarks.UnchangedBookmarks.Filter<MessageReceived>().Select(x => x.GetPayload<MessageReceivedTriggerPayload>());

        await StopWorkersAsync(removed, cancellationToken);
        await StartWorkersAsync(added, cancellationToken);
        await EnsureWorkersAsync(unchanged, cancellationToken);
    }
    
    private async Task StartWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.StartWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }

    private async Task StopWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.StopWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }

    private async Task EnsureWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.EnsureWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }
}