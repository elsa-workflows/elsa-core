using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Options;

/// <summary>
/// Represents options for invoking an activity.
/// </summary>
public class ActivityInvocationOptions
{
    /// <summary>Initializes a new instance of the <see cref="ActivityInvocationOptions"/> class.</summary>
    public ActivityInvocationOptions()
    {
        Input = new Dictionary<string, object>();
    }

    /// <summary>Initializes a new instance of the <see cref="ActivityInvocationOptions"/> class.</summary>
    /// <param name="owner">The activity execution context that owns this invocation.</param>
    /// <param name="tag">An optional tag that can be used to identify the invocation.</param>
    /// <param name="variables">The variables to declare in the activity execution context that will be created for this invocation.</param>
    /// <param name="existingActivityExecutionContext">An existing activity execution context to reuse.</param>
    /// <param name="input">Optional input to pass to the activity.</param>
    public ActivityInvocationOptions(
        ActivityExecutionContext? owner,
        object? tag,
        IEnumerable<Variable>? variables,
        ActivityExecutionContext? existingActivityExecutionContext = default,
        IDictionary<string, object>? input = default)
    {
        Owner = owner;
        Tag = tag;
        Variables = variables;
        ExistingActivityExecutionContext = existingActivityExecutionContext;
        Input = input ?? new Dictionary<string, object>();
    }

    /// <summary>The activity execution context that owns this invocation.</summary>
    public ActivityExecutionContext? Owner { get; set; }

    /// <summary>An optional tag that can be used to identify the invocation.</summary>
    public object? Tag { get; set; }

    /// <summary>The variables to declare in the activity execution context that will be created for this invocation.</summary>
    public IEnumerable<Variable>? Variables { get; set; }

    /// <summary>An existing activity execution context to reuse.</summary>
    public ActivityExecutionContext? ExistingActivityExecutionContext { get; set; }

    /// <summary>
    /// Optional input to pass to the activity.
    /// </summary>
    public IDictionary<string, object> Input { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this invocation.
    /// This represents the temporal/execution predecessor that directly triggered execution of this activity,
    /// distinct from the structural parent (<see cref="Owner"/>).
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this invocation.
    /// This is set when crossing workflow boundaries (e.g., via ExecuteWorkflow or DispatchWorkflow).
    /// For activities within the same workflow instance, this will be null.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The call stack depth of the scheduling activity execution context.
    /// This is used to calculate the call stack depth when the scheduling context is not present
    /// in ActivityExecutionContexts (e.g., for cross-workflow invocations).
    /// </summary>
    public int? SchedulingCallStackDepth { get; set; }
}