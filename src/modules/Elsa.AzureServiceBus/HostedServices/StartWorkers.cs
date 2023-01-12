using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.AzureServiceBus.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// Creates workers for each trigger & bookmark in response to updated workflow trigger indexes and bookmarks.
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
        var triggers = (await _triggerStore.FindByActivityTypeAsync(activityType, cancellationToken)).Select(x => DeserializePayload(x.Data!)).ToList();
        var bookmarks = (await _bookmarkStore.FindByActivityTypeAsync(activityType, cancellationToken)).Select(x => DeserializePayload(x.Data!)).ToList();
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