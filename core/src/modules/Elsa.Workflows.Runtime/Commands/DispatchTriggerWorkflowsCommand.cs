using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Represents a command that dispatches a workflow instance.
/// </summary>
public class DispatchTriggerWorkflowsCommand(string activityTypeName, object stimulus) : ICommand<Unit>
{
    public string ActivityTypeName { get; init; } = activityTypeName;
    public object Stimulus { get; init; } = stimulus;
    public string? CorrelationId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
}