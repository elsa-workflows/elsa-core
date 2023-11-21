using Elsa.Http.Models;

namespace Elsa.Http.Contracts;

/// <summary>
/// A handler that is invoked when authorizing an inbound HTTP request.
/// </summary>
public interface IHttpEndpointAuthorizationHandler
{
    /// <summary>
    /// Authorizes an inbound HTTP request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>True if the request is authorized, otherwise false.</returns>
    ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
}