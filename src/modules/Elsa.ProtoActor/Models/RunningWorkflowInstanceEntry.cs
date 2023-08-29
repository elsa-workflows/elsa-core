namespace Elsa.ProtoActor.Models;

/// <summary>
/// A snapshot of information about a running workflow instance.
/// </summary>
/// <param name="DefinitionId">The workflow definition id.</param>
/// <param name="Version">The workflow definition version.</param>
/// <param name="InstanceId">The workflow instance ID.</param>
/// <param name="CorrelationId">The workflow instance correlation ID.</param>
public record RunningWorkflowInstanceEntry(string DefinitionId, int Version, string InstanceId, string? CorrelationId);