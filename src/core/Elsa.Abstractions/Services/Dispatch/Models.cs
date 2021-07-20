﻿using Elsa.Services.Bookmarks;

namespace Elsa.Services
{
    public record TriggerWorkflowsRequest(string ActivityType, IBookmark Bookmark, object? Input = default, string? CorrelationId = default, string? WorkflowInstanceId = default, string? ContextId = default, string? TenantId = default);

    public record ExecuteWorkflowDefinitionRequest(string WorkflowDefinitionId, string? ActivityId = default, object? Input = default, string? CorrelationId = default, string? ContextId = default, string? TenantId = default);

    public record ExecuteWorkflowInstanceRequest(string WorkflowInstanceId, string? ActivityId, object? Input = default);
}