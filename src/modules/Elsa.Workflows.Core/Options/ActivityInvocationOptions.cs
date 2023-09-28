using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Options;

/// <summary>
/// Represents options for invoking an activity.
/// </summary>
public class ActivityInvocationOptions
{
    /// <summary>Initializes a new instance of the <see cref="ActivityInvocationOptions"/> class.</summary>
    public ActivityInvocationOptions()
    {
        
    }
    
    /// <summary>Initializes a new instance of the <see cref="ActivityInvocationOptions"/> class.</summary>
    /// <param name="owner">The activity execution context that owns this invocation.</param>
    /// <param name="tag">An optional tag that can be used to identify the invocation.</param>
    /// <param name="variables">The variables to declare in the activity execution context that will be created for this invocation.</param>
    /// <param name="reuseActivityExecutionContextId">The ID of an existing activity execution context to reuse.</param>
    public ActivityInvocationOptions(ActivityExecutionContext? owner, object? tag, IEnumerable<Variable>? variables, string? reuseActivityExecutionContextId = default)
    {
        Owner = owner;
        Tag = tag;
        Variables = variables;
        ReuseActivityExecutionContextId = reuseActivityExecutionContextId;
    }

    /// <summary>The activity execution context that owns this invocation.</summary>
    public ActivityExecutionContext? Owner { get; set; }

    /// <summary>An optional tag that can be used to identify the invocation.</summary>
    public object? Tag { get; set; }

    /// <summary>The variables to declare in the activity execution context that will be created for this invocation.</summary>
    public IEnumerable<Variable>? Variables { get; set; }

    /// <summary>The ID of an existing activity execution context to reuse.</summary>
    public string? ReuseActivityExecutionContextId { get; set; }
}