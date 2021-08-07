namespace Elsa.Services.Models
{
    public record WorkflowsQuery(string ActivityType, IBookmark? Bookmark, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default);
}