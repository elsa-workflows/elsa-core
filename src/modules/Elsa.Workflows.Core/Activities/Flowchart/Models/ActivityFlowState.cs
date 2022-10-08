using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

[DebuggerDisplay("ActivityId = {ActivityId}, ExecutionCount = {ExecutionCount}")]
public class ActivityFlowState
{
    [JsonConstructor]
    public ActivityFlowState()
    {
    }

    public ActivityFlowState(string activityId, long executionCount = 0)
    {
        ActivityId = activityId;
        ExecutionCount = executionCount;
    }
    
    public string ActivityId { get; set; } = default!;
    public long ExecutionCount { get; set; }
}