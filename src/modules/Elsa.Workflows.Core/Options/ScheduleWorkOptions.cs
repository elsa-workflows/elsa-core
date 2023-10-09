using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Options;

/// <summary>
/// Represents options for scheduling a work item.
/// </summary>
/// <param name="CompletionCallback">The callback to invoke when the work item has completed.</param>
/// <param name="Tag">A tag that can be used to identify the work item.</param>
/// <param name="Variables">A collection of variables to declare in the activity execution context that will be created for this work item.</param>
public record ScheduleWorkOptions(
    ActivityCompletionCallback? CompletionCallback = default, 
    object? Tag = default, 
    ICollection<Variable>? Variables = default, 
    ActivityExecutionContext? ExistingActivityExecutionContext = default, 
    bool PreventDuplicateScheduling = false,
    IDictionary<string, object>? Input = default);