using Elsa.Retention.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Retention.CleanupStrategies;

/// <summary>
///     Deletes <see cref="WorkflowExecutionLogRecord" />
/// </summary>
public class DeleteWorkflowExecutionRecordStrategy(IWorkflowExecutionLogStore store, ILogger<DeleteWorkflowExecutionRecordStrategy> logger) : IDeletionCleanupStrategy<WorkflowExecutionLogRecord>
{
    public async Task Cleanup(ICollection<WorkflowExecutionLogRecord> collection)
    {
        var filter = new WorkflowExecutionLogRecordFilter()
        {
            Ids = collection.Select(x => x.Id).ToList()
        };

        var deletedRecords = await store.DeleteManyAsync(filter);

        if (deletedRecords != collection.Count)
        {
            logger.LogWarning("Expected to delete {Expected} workflow execution records, actually deleted {Actual} workflow execution records", collection.Count, deletedRecords);
        }
    }
}