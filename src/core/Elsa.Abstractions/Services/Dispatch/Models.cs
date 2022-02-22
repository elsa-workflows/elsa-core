using Elsa.Abstractions.Multitenancy;
using Elsa.Models;

namespace Elsa.Services
{
    public record TriggerWorkflowsRequest(Tenant Tenant, string ActivityType, IBookmark Bookmark, WorkflowInput? Input = default, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default);

    public record ExecuteWorkflowDefinitionRequest(Tenant Tenant, string WorkflowDefinitionId, string? ActivityId = default, WorkflowInput? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default);

    public record ExecuteWorkflowInstanceRequest(Tenant Tenant, string WorkflowInstanceId, string? ActivityId = default, WorkflowInput? Input = default);
}