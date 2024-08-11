using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes <see cref="WorkflowExecutionLogRecord" />
/// </summary>
public class DeleteWorkflowExecutionRecordStrategy : IDeletionCleanupStrategy<WorkflowExecutionLogRecord>
{
    private readonly ILogger<DeleteWorkflowExecutionRecordStrategy> _logger;
    private readonly IWorkflowExecutionLogStore _store;

    public DeleteWorkflowExecutionRecordStrategy(IWorkflowExecutionLogStore store, ILogger<DeleteWorkflowExecutionRecordStrategy> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Cleanup(ICollection<WorkflowExecutionLogRecord> collection)
    {
        WorkflowExecutionLogRecordFilter filter = new()
        {
            Ids = collection.Select(x => x.Id).ToList()
        };

        long deletedRecords = await _store.DeleteManyAsync(filter);

        if (deletedRecords != collection.Count)
        {
            _logger.LogWarning("Expected to delete {Expected} workflow execution records, actually deleted {Actual} workflow execution records", collection.Count, deletedRecords);
        }
    }
}