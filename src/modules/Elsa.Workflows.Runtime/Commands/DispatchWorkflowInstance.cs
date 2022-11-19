using Elsa.Mediator.Services;

namespace Elsa.Workflows.Runtime.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public record DispatchWorkflowInstance(
    string InstanceId, 
    string? BookmarkId = default,
    string? ActivityId = default,
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default) : ICommand;

// ReSharper disable once UnusedType.Global