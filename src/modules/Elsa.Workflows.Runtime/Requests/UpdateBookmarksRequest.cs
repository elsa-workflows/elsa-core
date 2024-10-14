using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Published when bookmarks needs to be updated.
/// </summary>
/// <param name="WorkflowInstanceId">The workflow instance ID.</param>
/// <param name="Diff">A diff of the bookmarks.</param>
/// <param name="CorrelationId">The correlation ID, if any.</param>
public record UpdateBookmarksRequest(WorkflowExecutionContext WorkflowExecutionContext, Diff<Bookmark> Diff, string? CorrelationId = default);