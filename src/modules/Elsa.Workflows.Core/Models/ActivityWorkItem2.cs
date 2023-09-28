using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a work item that can be scheduled for execution.
/// </summary>
public class ActivityWorkItem
{
    /// <summary>
    /// Creates a new instance of the <see cref="ActivityWorkItem"/> class.
    /// </summary>
    public ActivityWorkItem(IActivity activity, ActivityExecutionContext? owner = default, object? tag = default, IEnumerable<Variable>? variables = default, string? reuseActivityExecutionContextId = default)
    {
        Activity = activity;
        Owner = owner;
        Tag = tag;
        Variables = variables;
        ReuseActivityExecutionContextId = reuseActivityExecutionContextId;
    }

    public IActivity Activity { get; }
    public ActivityExecutionContext? Owner { get; set; }
    public object? Tag { get; set; }
    public IEnumerable<Variable>? Variables { get; set; }
    public string? ReuseActivityExecutionContextId { get; set; }
}