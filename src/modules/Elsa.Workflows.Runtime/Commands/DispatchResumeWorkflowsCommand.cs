using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

public record DispatchResumeWorkflowsCommand(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default, 
    IDictionary<string, object>? Input = default) : ICommand<Unit>;