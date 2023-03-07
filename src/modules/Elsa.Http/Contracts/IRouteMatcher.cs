using Microsoft.AspNetCore.Routing;

namespace Elsa.Http.Contracts;

/// <summary>
/// Matches a given request path against the specified route template.
/// </summary>
public interface IRouteMatcher
{
    /// <summary>
    /// Matches a given request path against the specified route template.
    /// </summary>
    RouteValueDictionary? Match(string routeTemplate, string requestPath);
}