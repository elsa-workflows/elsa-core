using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Published when bookmarks needs to be updated.
/// </summary>
/// <param name="WorkflowExecutionContext">The workflow execution context.</param>
/// <param name="Diff">A diff of the bookmarks.</param>
/// <param name="CorrelationId">The correlation ID, if any.</param>
public record UpdateBookmarksRequest(WorkflowExecutionContext WorkflowExecutionContext, Diff<Bookmark> Diff, string? CorrelationId);