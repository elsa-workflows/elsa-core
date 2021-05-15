using System.Collections.Generic;
using Elsa.Bookmarks;
using MediatR;

namespace Elsa.Dispatch
{
    public record TriggerWorkflowsRequest(string ActivityType, IBookmark Bookmark, IBookmark Trigger, object? Input = default, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default, bool Execute = true) : IRequest<TriggerWorkflowsResponse>;
    public record ExecuteWorkflowDefinitionRequest(string WorkflowDefinitionId, string? ActivityId = default, object? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default, bool Execute = true) : IRequest<ExecuteWorkflowDefinitionResponse>;
    public record ExecuteWorkflowInstanceRequest(string WorkflowInstanceId, string ActivityId, object? Input = default, bool Execute = true) : IRequest<Unit>;
    public record TriggerWorkflowsResponse(ICollection<PendingWorkflow> PendingWorkflows);
    public record ExecuteWorkflowDefinitionResponse(PendingWorkflow? PendingWorkflow = default);

    public record PendingWorkflow(string WorkflowInstanceId, string ActivityId);

}