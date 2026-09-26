using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;

internal record WorkflowInstanceRow(
    string WorkflowInstanceId,
    string? CorrelationId,
    string WorkflowDefinitionName,
    int Version,
    string? Name,
    WorkflowStatus Status,
    WorkflowSubStatus SubStatus,
    int IncidentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? FinishedAt
);