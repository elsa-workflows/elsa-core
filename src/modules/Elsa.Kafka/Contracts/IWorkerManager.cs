using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Kafka;

public interface IWorkerManager
{
    Task UpdateWorkersAsync(CancellationToken cancellationToken = default);
    void StopWorkers();
    Task BindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);
    Task BindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default);
    IWorker? GetWorker(string consumerDefinitionId);
}