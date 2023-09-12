using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Options;

/// <summary>
/// Represents options for invoking an activity.
/// </summary>
/// <param name="Owner">The activity execution context that owns this invocation.</param>
/// <param name="Tag">An optional tag that can be used to identify the invocation.</param>
/// <param name="Variables">The variables to declare in the activity execution context that will be created for this invocation.</param>
public record ActivityInvocationOptions(ActivityExecutionContext? Owner, object? Tag, IEnumerable<Variable>? Variables, string? ReuseActivityExecutionContextId = default);