using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Commands;

/// <summary>
/// A command to dispatch a workflow instance.
/// </summary>
[PublicAPI]
public class DispatchWorkflowInstanceCommand(string instanceId) : ICommand<Unit>
{
    public string InstanceId { get; init; } = instanceId;
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? CorrelationId { get; set; }
}