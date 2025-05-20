using Elsa.Kafka.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Kafka.Implementations;

public class WorkerTopicSubscriber(ITriggerStore triggerStore, IBookmarkStore bookmarkStore, IWorkerManager workerManager) : IWorkerTopicSubscriber
{
    private static readonly string MessageReceivedActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();

    public async Task UpdateTopicSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var triggers = await GetStoredTriggersAsync(cancellationToken);
        var bookmarks = await GetStoredBookmarksAsync(cancellationToken);
        await workerManager.BindTriggersAsync(triggers, cancellationToken);
        await workerManager.BindBookmarksAsync(bookmarks, cancellationToken);
    }
    
    private async Task<IEnumerable<StoredTrigger>> GetStoredTriggersAsync(CancellationToken cancellationToken)
    {
        var triggerFilter = new TriggerFilter
        {
            Name = MessageReceivedActivityTypeName
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }
    
    private async Task<IEnumerable<StoredBookmark>> GetStoredBookmarksAsync(CancellationToken cancellationToken)
    {
        var bookmarkFilter = new BookmarkFilter
        {
            Name = MessageReceivedActivityTypeName
        };
        return await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken);
    }
}