using Elsa.Workflows.Core;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Get;

internal class Request
{
    public string Id { get; set; } = default!;
}

internal class Response
{
    public string Id { get; set; }
    public string DefinitionId { get; init; } = default!;
    public string DefinitionVersionId { get; init; } = default!;
    public int Version { get; init; }
    public WorkflowState WorkflowState { get; set; } = default!;
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }

    [Obsolete("Use WorkflowState.Properties instead.")]
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    [Obsolete("Use WorkflowState.Fault instead.")]
    public WorkflowFaultState? Fault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
}