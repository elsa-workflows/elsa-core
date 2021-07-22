namespace Elsa.Services.Models
{
    public record StartableWorkflowsQuery(string WorkflowDefinitionId, string? ActivityId = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default);
}