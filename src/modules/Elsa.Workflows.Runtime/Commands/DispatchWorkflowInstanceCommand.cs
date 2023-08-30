using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// A command to dispatch a workflow instance.
/// </summary>
/// <param name="InstanceId"></param>
/// <param name="BookmarkId"></param>
/// <param name="ActivityId"></param>
/// <param name="ActivityNodeId"></param>
/// <param name="ActivityInstanceId"></param>
/// <param name="ActivityHash"></param>
/// <param name="Input"></param>
/// <param name="CorrelationId"></param>
[PublicAPI]
public record DispatchWorkflowInstanceCommand(
    string InstanceId, 
    string? BookmarkId = default,
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default) : ICommand<Unit>;