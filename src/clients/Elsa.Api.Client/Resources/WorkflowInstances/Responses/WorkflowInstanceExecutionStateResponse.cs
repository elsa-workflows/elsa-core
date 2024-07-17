using Elsa.Api.Client.Resources.WorkflowInstances.Enums;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Responses;

/// Represents the response containing the last updated timestamp of a workflow instance.
public record WorkflowInstanceExecutionStateResponse(WorkflowStatus Status, WorkflowSubStatus WorkflowSubStatus, DateTimeOffset UpdatedAt);