using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Represents a command that dispatches a workflow instance.
/// </summary>
/// <param name="activityTypeName">The name of the activity type to dispatch.</param>
/// <param name="stimulus">The payload to match the bookmark.</param>
public class DispatchResumeWorkflowsCommand(string activityTypeName, object stimulus) : ICommand<Unit>
{
    /// <summary>The name of the activity type to dispatch.</summary>
    public string ActivityTypeName { get; init; } = activityTypeName;

    /// <summary>The payload to match the bookmark.</summary>
    public object Stimulus { get; init; } = stimulus;

    /// <summary>The correlation ID of the workflow instance to dispatch.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>The ID of the workflow instance to dispatch.</summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>The ID of the activity instance to dispatch.</summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>The input to pass to the activity.</summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// Any properties to attach to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
}