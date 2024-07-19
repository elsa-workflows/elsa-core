using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// Extracts activity execution log records.
public class ActivityExecutionRecordExtractor(IActivityExecutionMapper activityExecutionMapper) : IActivityExecutionRecordExtractor
{
    /// <inheritdoc />
    public IEnumerable<ActivityExecutionRecord> ExtractWorkflowExecutionLogs(WorkflowExecutionContext context)
    {
        var activityExecutionContexts = context.ActivityExecutionContexts;
        return activityExecutionContexts.Select(activityExecutionMapper.Map).ToList();
    }
}