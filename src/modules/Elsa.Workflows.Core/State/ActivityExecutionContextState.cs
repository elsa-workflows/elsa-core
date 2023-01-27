using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.State;

/// <summary>
/// A serializable shape of <see cref="ActivityExecutionContext"/>.
/// </summary>
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
    public string ScheduledActivityNodeId { get; set; } = default!;
    public string? OwnerActivityNodeId { get; set; }
    public PropertyBag Properties { get; set; } = new();
    //public RegisterState Register { get; set; } = default!;
}