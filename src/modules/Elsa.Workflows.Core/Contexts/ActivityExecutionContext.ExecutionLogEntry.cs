using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public partial class ActivityExecutionContext
{
    /// <summary>
    /// Adds a new <see cref="WorkflowExecutionLogEntry"/> to the execution log of the current <see cref="Workflows.WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="message">The message of the event.</param>
    /// <param name="source">The source of the activity. For example, the source file name and line number in case of composite activities.</param>
    /// <param name="payload">Any contextual data related to this event.</param>
    /// <param name="includeActivityState">True to include activity state with this event, false otherwise.</param>
    /// <returns>Returns the created <see cref="WorkflowExecutionLogEntry"/>.</returns>
    public WorkflowExecutionLogEntry AddExecutionLogEntry(string eventName, string? message = default, string? source = default, object? payload = default, bool includeActivityState = false)
    {
        var activityState = includeActivityState ? ActivityState : default;

        var logEntry = new WorkflowExecutionLogEntry(
            Id,
            ParentActivityExecutionContext?.Id,
            Activity.Id,
            Activity.Type,
            Activity.Version,
            Activity.Name,
            NodeId,
            activityState,
            _systemClock.UtcNow,
            WorkflowExecutionContext.ExecutionLogSequence++,
            eventName,
            message,
            source ?? Activity.GetSource(),
            payload);

        WorkflowExecutionContext.ExecutionLog.Add(logEntry);
        return logEntry;
    }
}