using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Models;

public class ScheduledActivityOptions
{
    public string? CompletionCallback { get; set; }
    public object? Tag { get; set; }
    public ICollection<Variable>? Variables { get; set; }
    public string? ExistingActivityInstanceId { get; set; }
    public bool PreventDuplicateScheduling { get; set; }
    public IDictionary<string,object>? Input { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this activity.
    /// This represents the temporal/execution predecessor that directly triggered execution of this activity.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }
}