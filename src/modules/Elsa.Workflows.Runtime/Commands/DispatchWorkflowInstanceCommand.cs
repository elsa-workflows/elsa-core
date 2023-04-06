using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Workflows.Runtime.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public record DispatchWorkflowInstanceCommand(
    string InstanceId, 
    string? BookmarkId = default,
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default) : ICommand<Unit>;