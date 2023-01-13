using Elsa.Mediator.Models;
using Elsa.Mediator.Services;

namespace Elsa.Workflows.Runtime.Commands;

// ReSharper disable once UnusedType.Global
public record DispatchTriggerWorkflowsCommand(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default, 
    IDictionary<string, object>? Input = default) : ICommand<Unit>;