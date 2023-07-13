using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Represents a summary view of a <see cref="WorkflowInstance"/>.
/// </summary>
[PublicAPI]
public record WorkflowInstanceSummary
{
    /// <summary>The ID of the workflow instance.</summary>
    public string Id { get; set; } = default!;

    /// <summary>The ID of the workflow definition.</summary>
    public string DefinitionId { get; set; } = default!;

    /// <summary>The version ID of the workflow definition.</summary>
    public string DefinitionVersionId { get; set; } = default!;

    /// <summary>The version of the workflow definition.</summary>
    public int Version { get; set; }

    /// <summary>The status of the workflow instance.</summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>The sub-status of the workflow instance.</summary>
    public WorkflowSubStatus SubStatus { get; set; }

    /// <summary>The ID of the workflow instance.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>The name of the workflow instance.</summary>
    public string? Name { get; set; }

    /// <summary>The timestamp when the workflow instance was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The timestamp when the workflow instance was last executed.</summary>
    public DateTimeOffset? LastExecutedAt { get; set; }

    /// <summary>The timestamp when the workflow instance was finished.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>The timestamp when the workflow instance was cancelled.</summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>The timestamp when the workflow instance was faulted.</summary>
    public DateTimeOffset? FaultedAt { get; set; }
}