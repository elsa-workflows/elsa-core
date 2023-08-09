using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// Represents a command that dispatches a workflow instance.
/// </summary>
/// <param name="ActivityTypeName">The name of the activity type to dispatch.</param>
/// <param name="BookmarkPayload">The payload to match the bookmark.</param>
/// <param name="CorrelationId">The correlation ID of the workflow instance to dispatch.</param>
/// <param name="WorkflowInstanceId">The ID of the workflow instance to dispatch.</param>
/// <param name="ActivityInstanceId">The ID of the activity instance to dispatch.</param>
/// <param name="Input">The input to pass to the activity.</param>
public record DispatchResumeWorkflowsCommand(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default, 
    string? WorkflowInstanceId = default,
    string? ActivityInstanceId = default,
    IDictionary<string, object>? Input = default) : ICommand<Unit>;