using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Contracts;

/// <summary>
/// Optional host hook invoked after HTTP trigger and bookmark lookup found no match.
/// Combined Server+Studio hosts use this to fall through to Studio pages without
/// skipping lookup or disabling existing HTTP endpoints.
/// </summary>
/// <remarks>
/// Middleware resolves a single handler with <c>GetService</c>. If several implementations
/// are registered, the last registration wins; there is no ordered handler chain.
/// Returning <c>false</c> preserves the middleware's existing unmatched behavior: 404 when
/// <c>BasePath</c> is set, otherwise <c>next</c>. Host-specific page matching is not part of
/// this contract. Matching triggers and bookmarks are rejected before this hook runs, including
/// during administrative pause.
/// </remarks>
public interface IHttpWorkflowUnmatchedHandler
{
    /// <summary>
    /// Attempts to handle an unmatched HTTP workflow request.
    /// Return <c>true</c> if the request was handled, including by calling <paramref name="next"/>.
    /// Return <c>false</c> to keep the middleware's existing unmatched behavior.
    /// </summary>
    Task<bool> HandleAsync(HttpContext httpContext, RequestDelegate next, CancellationToken cancellationToken);
}
