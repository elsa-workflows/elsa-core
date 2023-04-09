using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
public class StartWorkers : IHostedService
{
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkPayloadSerializer _serializer;
    private readonly IWorkerManager _workerManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public StartWorkers(ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IBookmarkPayloadSerializer serializer, IWorkerManager workerManager)
    {
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
        _serializer = serializer;
        _workerManager = workerManager;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var activityType = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();
        var triggerFilter = new TriggerFilter { Name = activityType};
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken)).Select(x => DeserializePayload(x.Data!)).ToList();
        var bookmarkFilter = new BookmarkFilter { ActivityTypeName = activityType };
        var bookmarks = (await _bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).Select(x => DeserializePayload(x.Data!)).ToList();
        var payloads = triggers.Concat(bookmarks).ToList();

        await EnsureWorkersAsync(payloads, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private MessageReceivedTriggerPayload DeserializePayload(string payload) => _serializer.Deserialize<MessageReceivedTriggerPayload>(payload);

    private async Task EnsureWorkersAsync(IEnumerable<MessageReceivedTriggerPayload> payloads, CancellationToken cancellationToken)
    {
        foreach (var payload in payloads) await _workerManager.EnsureWorkerAsync(payload.QueueOrTopic, payload.Subscription, cancellationToken);
    }
}