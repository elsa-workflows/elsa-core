using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes activity execution records.
/// </summary>
public class DeleteActivityExecutionRecordStrategy(IActivityExecutionStore store, ILogger<DeleteActivityExecutionRecordStrategy> logger) : IDeletionCleanupStrategy<ActivityExecutionRecord>
{
    public async Task Cleanup(ICollection<ActivityExecutionRecord> collection)
    {
        var filter = new ActivityExecutionRecordFilter()
        {
            Ids = collection.Select(x => x.Id).ToList()
        };

        var deletedRecords = await store.DeleteManyAsync(filter);

        if (deletedRecords != collection.Count)
        {
            logger.LogWarning("Expected to delete {Expected} activity execution records, actually deleted {Actual} activity execution records", collection.Count, deletedRecords);
        }
    }
}