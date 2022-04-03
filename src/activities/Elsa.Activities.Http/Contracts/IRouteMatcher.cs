using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Activities.Http.Contracts;

/// <summary>
/// Matches a a given request path against the specified route template.
/// </summary>
public interface IRouteMatcher
{
    /// <summary>
    /// Matches a a given request path against the specified route template.
    /// </summary>
    RouteValueDictionary? Match(string routeTemplate, string requestPath, IQueryCollection query);
}