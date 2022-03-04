using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Modules.AzureServiceBus.Contracts;
using Elsa.Modules.AzureServiceBus.Models;
using Elsa.Persistence.Extensions;
using Elsa.Runtime.Models;
using Elsa.Runtime.Notifications;

namespace Elsa.Modules.AzureServiceBus.Handlers;

/// <summary>
/// Creates workers for each trigger & bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
public class UpdateWorkers : INotificationHandler<WorkflowTriggersIndexed>, INotificationHandler<WorkflowBookmarksDeleted>, INotificationHandler<WorkflowBookmarksSaved>
{
    private readonly IPayloadSerializer _serializer;
    private readonly IWorkerManager _workerManager;

    public UpdateWorkers(IPayloadSerializer serializer, IWorkerManager workerManager)
    {
        _serializer = serializer;
        _workerManager = workerManager;
    }

    /// <summary>
    /// Adds, updates and removes workers based on added and removed triggers.
    /// </summary>
    public async Task HandleAsync(WorkflowTriggersIndexed notification, CancellationToken cancellationToken)
    {
        var added = notification.IndexedWorkflowTriggers.AddedTriggers.Filter<MessageReceived>().Select(x => DeserializePayload(x.Payload!));
        var removed = notification.IndexedWorkflowTriggers.RemovedTriggers.Filter<MessageReceived>().Select(x => DeserializePayload(x.Payload!));

        await StopWorkersAsync(removed, cancellationToken);
        await StartWorkersAsync(added, cancellationToken);
    }

    /// <summary>
    /// Removes workers based on removed bookmarks. 
    /// </summary>
    public async Task HandleAsync(WorkflowBookmarksDeleted notification, CancellationToken cancellationToken)
    {
        var payloads = notification.Bookmarks.Filter<MessageReceived>().Select(x => DeserializePayload(x.Payload!));
        await StopWorkersAsync(payloads, cancellationToken);
    }

    /// <summary>
    /// Starts workers based on added bookmarks. 
    /// </summary>
    public async Task HandleAsync(WorkflowBookmarksSaved notification, CancellationToken cancellationToken)
    {
        var payloads = notification.Bookmarks.Filter<MessageReceived>().Select(x => DeserializePayload(x.Payload!));
        await StartWorkersAsync(payloads, cancellationToken);
    }

    private MessageReceivedTriggerPayload DeserializePayload(string payload) => _serializer.Deserialize<MessageReceivedTriggerPayload>(payload);
    private ICollection<MessageReceivedTriggerPayload> DeserializePayloads(IEnumerable<string> payloads) => payloads.Select(_serializer.Deserialize<MessageReceivedTriggerPayload>).ToList();

    private async Task StartWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.StartWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }

    private async Task StopWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.StopWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }
}