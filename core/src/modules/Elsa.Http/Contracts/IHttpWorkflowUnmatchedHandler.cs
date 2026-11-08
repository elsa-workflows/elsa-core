using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Contracts;

/// <summary>
/// Optional host hook invoked after HTTP trigger and bookmark lookup found no match.
/// Combined Server+Studio hosts use this to fall through to Studio pages without
/// skipping lookup or disabling existing HTTP endpoints.
/// </summary>
public interface IHttpWorkflowUnmatchedHandler
{
    /// <summary>
    /// Attempts to handle an unmatched HTTP workflow request.
    /// Return <c>true</c> if the request was handled, including by calling <paramref name="next"/>.
    /// Return <c>false</c> to keep the middleware's unmatched 404 behavior.
    /// </summary>
    Task<bool> HandleAsync(HttpContext httpContext, RequestDelegate next, CancellationToken cancellationToken);
}
