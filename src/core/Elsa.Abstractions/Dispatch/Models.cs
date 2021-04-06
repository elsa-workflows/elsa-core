using Elsa.Bookmarks;
using MediatR;

namespace Elsa.Dispatch
{
    public record TriggerWorkflowsRequest(string ActivityType, IBookmark Bookmark, IBookmark Trigger, object? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default) : IRequest<int>;
    public record ExecuteWorkflowDefinitionRequest(string WorkflowDefinitionId, string? ActivityId = default, object? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default) : IRequest<Unit>;
    public record ExecuteWorkflowInstanceRequest(string WorkflowInstanceId, string ActivityId, object? Input = default) : IRequest<Unit>;

}