using Elsa.Http.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.ServerAndStudio.Web;

/// <summary>
/// After HTTP trigger/bookmark lookup misses, fall through only to exact Studio workflow pages.
/// Existing HTTP endpoints under <c>/workflows/definitions/...</c> and <c>/workflows/instances/...</c>
/// still start and resume because lookup runs first. Unmatched non-page paths stay 404.
/// An HTTP workflow registered at an exact Studio page URL hides that Studio page for the matching
/// method; choose a distinct HTTP path if both are needed.
/// </summary>
internal sealed class CombinedStudioUnmatchedHttpHandler : IHttpWorkflowUnmatchedHandler
{
    public async Task<bool> HandleAsync(HttpContext httpContext, RequestDelegate next, CancellationToken cancellationToken)
    {
        if (!IsStudioWorkflowPage(httpContext.Request.Path.Value))
        {
            return false;
        }

        await next(httpContext);
        return true;
    }

    internal static bool IsStudioWorkflowPage(string? path)
    {
        var segments = (path ?? string.Empty).Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 2
            && string.Equals(segments[0], "workflows", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(segments[1], "definitions", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segments[1], "instances", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (segments.Length == 4
            && string.Equals(segments[0], "workflows", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[1], "definitions", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[3], "edit", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (segments.Length == 4
            && string.Equals(segments[0], "workflows", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[1], "instances", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[3], "view", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
