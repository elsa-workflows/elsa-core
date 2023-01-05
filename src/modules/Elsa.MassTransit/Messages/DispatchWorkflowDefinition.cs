using Elsa.Common.Models;

namespace Elsa.MassTransit.Messages;

// ReSharper disable once InconsistentNaming
public interface DispatchWorkflowDefinition
{
    string DefinitionId { get; set; }
    VersionOptions VersionOptions { get; set; }
    IDictionary<string, object>? Input { get; set; }
    string? CorrelationId { get; set; }
}