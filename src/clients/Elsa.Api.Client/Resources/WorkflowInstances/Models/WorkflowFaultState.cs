using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Holds information about a workflow fault.
/// </summary>
/// <param name="Exception">The exception that occurred</param>
/// <param name="Message">A description about the fault. Usually the exception message, if there waa an exception.</param>
/// <param name="FaultedActivityId">The ID of the activity that caused the workflow to fault.</param>
[PublicAPI]
public record WorkflowFaultState(ExceptionState? Exception, string Message, string? FaultedActivityId)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowFaultState"/> class.
    /// </summary>
    [JsonConstructor]
    public WorkflowFaultState() : this(default, default!, default)
    {
    }
}