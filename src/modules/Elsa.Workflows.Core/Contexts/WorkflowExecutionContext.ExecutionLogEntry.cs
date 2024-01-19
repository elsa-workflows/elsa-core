using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public partial class WorkflowExecutionContext
{
    /// <summary>
    /// Adds a new <see cref="WorkflowExecutionLogEntry"/> to the execution log of the current <see cref="Workflows.WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="message">The message of the event.</param>
    /// <param name="payload">Any contextual data related to this event.</param>
    /// <returns>Returns the created <see cref="WorkflowExecutionLogEntry"/>.</returns>
    public WorkflowExecutionLogEntry AddExecutionLogEntry(string eventName, string? message = default, object? payload = default)
    {
        var logEntry = new WorkflowExecutionLogEntry(
            Id,
            default,
            Workflow.Id,
            Workflow.Type,
            Workflow.Identity.Version,
            Workflow.Name,
            Workflow.Identity.Id,
            default,
            SystemClock.UtcNow,
            ExecutionLogSequence++,
            eventName,
            message,
            Workflow.GetSource(),
            payload);

        ExecutionLog.Add(logEntry);
        return logEntry;
    }
}