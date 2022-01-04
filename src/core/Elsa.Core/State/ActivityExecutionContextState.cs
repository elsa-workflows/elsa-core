namespace Elsa.State;

public class ActivityExecutionContextState
{
    public ActivityExecutionContextState()
    {
    }

    public string Id { get; set; } = default!;
    public ActivityExecutionContextState? ParentActivityExecutionContext { get; set; }
    public string ScheduledActivityId { get; set; } = default!;
    public string? OwnerActivityId { get; set; }
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public RegisterState Register { get; set; } = default!;
}