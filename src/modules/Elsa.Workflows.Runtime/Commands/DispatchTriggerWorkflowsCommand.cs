using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Represents a command that dispatches a workflow instance.
/// </summary>
/// <param name="ActivityTypeName"></param>
/// <param name="BookmarkPayload"></param>
/// <param name="CorrelationId"></param>
/// <param name="WorkflowInstanceId"></param>
/// <param name="Input"></param>
public record DispatchTriggerWorkflowsCommand(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default,
    string? WorkflowInstanceId = default,
    IDictionary<string, object>? Input = default) : ICommand<Unit>;