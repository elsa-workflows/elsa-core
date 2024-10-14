using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Contracts;
using Elsa.AzureServiceBus.Models;
using Elsa.Extensions;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.AzureServiceBus.HostedServices;

/// <summary>
/// Creates workers for each trigger &amp; bookmark in response to updated workflow trigger indexes and bookmarks.
/// </summary>
[UsedImplicitly]
public class StartWorkers(IServiceScopeFactory scopeFactory) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var triggerStore = scope.ServiceProvider.GetRequiredService<ITriggerStore>();
        var bookmarkStore = scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
        var workerManager = scope.ServiceProvider.GetRequiredService<IWorkerManager>();
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

        await EnsureWorkersAsync(workerManager, stimuli, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureWorkersAsync(IWorkerManager workerManager, IEnumerable<MessageReceivedStimulus> stimuli, CancellationToken cancellationToken)
    {
        foreach (var stimulus in stimuli) await workerManager.StartWorkerAsync(stimulus.QueueOrTopic, stimulus.Subscription, cancellationToken);
    }
}