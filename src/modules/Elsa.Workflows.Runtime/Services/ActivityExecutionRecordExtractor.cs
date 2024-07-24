using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// Extracts activity execution log records.
public class ActivityExecutionRecordExtractor(IActivityExecutionMapper activityExecutionMapper) : ILogRecordExtractor<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public IEnumerable<ActivityExecutionRecord> ExtractLogRecords(WorkflowExecutionContext context)
    {
        var activityExecutionContexts = context.ActivityExecutionContexts;
        return activityExecutionContexts.Select(activityExecutionMapper.Map).ToList();
    }
}