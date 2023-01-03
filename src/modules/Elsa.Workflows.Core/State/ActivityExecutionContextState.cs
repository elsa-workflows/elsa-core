using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.State;

public class ActivityExecutionContextState
{
    // ReSharper disable once EmptyConstructor
    // Required for JSON serialization configured with reference handling.
    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityExecutionContextState()
    {
    }

    public string Id { get; set; } = default!;
    public string? ParentContextId { get; set; }
    public string ScheduledActivityId { get; set; } = default!;
    public string? OwnerActivityId { get; set; }
    public PropertyBag Properties { get; set; } = new();
    //public RegisterState Register { get; set; } = default!;
}