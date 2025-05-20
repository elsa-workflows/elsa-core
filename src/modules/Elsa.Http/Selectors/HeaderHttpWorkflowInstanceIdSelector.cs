using Elsa.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Selectors;

/// <summary>
/// Returns the workflow instance ID from the <c>X-Workflow-Instance-ID</c> header, if any.
/// </summary>
public class HeaderHttpWorkflowInstanceIdSelector : HttpWorkflowInstanceIdSelectorBase
{
    /// <inheritdoc />
    protected override string? GetWorkflowInstanceId(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-Workflow-Instance-ID"].FirstOrDefault();
    }
}