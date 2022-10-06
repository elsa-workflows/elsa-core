using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

[DebuggerDisplay("Activity = {ActivityId}, HasExecuted = {HasExecuted}")]
public class ActivityFlowState
{
    [JsonConstructor]
    public ActivityFlowState()
    {
    }

    public ActivityFlowState(string activityId, bool hasExecuted = false)
    {
        ActivityId = activityId;
        HasExecuted = hasExecuted;
    }
    
    public string ActivityId { get; set; } = default!;
    public bool HasExecuted { get; set; }
}