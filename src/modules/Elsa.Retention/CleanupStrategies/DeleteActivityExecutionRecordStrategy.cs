using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes activity execution records.
/// </summary>
public class DeleteActivityExecutionRecordStrategy : IDeletionCleanupStrategy<ActivityExecutionRecord>
{
    private readonly ILogger<DeleteActivityExecutionRecordStrategy> _logger;
    private readonly IActivityExecutionStore _store;

    public DeleteActivityExecutionRecordStrategy(IActivityExecutionStore store, ILogger<DeleteActivityExecutionRecordStrategy> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Cleanup(ICollection<ActivityExecutionRecord> collection)
    {
        ActivityExecutionRecordFilter filter = new()
        {
            Ids = collection.Select(x => x.Id).ToList()
        };

        long deletedRecords = await _store.DeleteManyAsync(filter);

        if (deletedRecords != collection.Count)
        {
            _logger.LogWarning("Expected to delete {Expected} activity execution records, actually deleted {Actual} activity execution records", collection.Count, deletedRecords);
        }
    }
}