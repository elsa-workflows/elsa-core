namespace Elsa.Workflows.Core.State
{
    /// <summary>
    /// Holds information about a workflow fault.
    /// </summary>
    /// <param name="Exception">The exception that occurred</param>
    /// <param name="Message">A description about the fault. Usually the exception message, if there waa an exception.</param>
    /// <param name="FaultedActivityId">The ID of the activity that caused the workflow to fault.</param>
    public record WorkflowFaultState(ExceptionState? Exception, string Message, string? FaultedActivityId);
}