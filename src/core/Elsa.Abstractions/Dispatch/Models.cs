using Elsa.Bookmarks;

namespace Elsa.Dispatch
{
    public record ExecuteCorrelatedWorkflowRequest(string CorrelationId, IBookmark Bookmark, IBookmark Trigger, string ActivityType, object? Input = default, string? ContextId = default, string? TenantId = default);
    public record ExecuteWorkflowDefinitionRequest(string WorkflowDefinitionId, string? ActivityId = default, object? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default);
    public record ExecuteWorkflowInstanceRequest(string WorkflowInstanceId, string ActivityId, object? Input = default);

}