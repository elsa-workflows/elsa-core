namespace Elsa.Workflows.Core.State;

public class ActivityExecutionContextState
{
    // ReSharper disable once EmptyConstructor
    // Required for JSON serialization configured with reference handling.
    public ActivityExecutionContextState()
    {
    }

    public string Id { get; set; } = default!;
    public string? ParentContextId { get; set; }
    public string ScheduledActivityId { get; set; } = default!;
    public string? OwnerActivityId { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public RegisterState Register { get; set; } = default!;
}