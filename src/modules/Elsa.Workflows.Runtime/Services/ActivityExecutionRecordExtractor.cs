using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Extracts activity execution log records.
/// </summary>
public class ActivityExecutionRecordExtractor(IActivityExecutionMapper activityExecutionMapper) : ILogRecordExtractor<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> ExtractLogRecordsAsync(WorkflowExecutionContext context)
    {
        var activityExecutionContexts = context.ActivityExecutionContexts;
        var tasks = activityExecutionContexts.Select(activityExecutionMapper.MapAsync).ToList();
        return await Task.WhenAll(tasks);
    }
}