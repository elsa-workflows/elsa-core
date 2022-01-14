using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;

namespace Elsa.Services
{
    public record TriggerWorkflowsRequest(string ActivityType, IBookmark Bookmark, WorkflowInput? Input = default, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default, Tenant? Tenant = default);

    public record ExecuteWorkflowDefinitionRequest(string WorkflowDefinitionId, string? ActivityId = default, WorkflowInput? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default, Tenant? Tenant = default);

    public record ExecuteWorkflowInstanceRequest(string WorkflowInstanceId, string? ActivityId = default, WorkflowInput? Input = default, Tenant? Tenant = default);
}