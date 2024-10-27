using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.AzureServiceBus.Tasks;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class StartWorkers(ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IWorkerManager workerManager) : BackgroundTask
{
    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var activityType = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();
        var triggerFilter = new TriggerFilter
        {
            Name = activityType
        };
        var triggerStimuli = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).Select(x => x.GetPayload<MessageReceivedStimulus>()).ToList();
        var bookmarkFilter = new BookmarkFilter
        {
            ActivityTypeName = activityType
        };
        var bookmarkStimuli = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).Select(x => x.GetPayload<MessageReceivedStimulus>()).ToList();
        var stimuli = triggerStimuli.Concat(bookmarkStimuli).ToList();

        await EnsureWorkersAsync(stimuli, cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureWorkersAsync(IEnumerable<MessageReceivedStimulus> stimuli, CancellationToken cancellationToken)
    {
        foreach (var stimulus in stimuli)
            await workerManager.StartWorkerAsync(stimulus.QueueOrTopic, stimulus.Subscription, cancellationToken);
    }
}