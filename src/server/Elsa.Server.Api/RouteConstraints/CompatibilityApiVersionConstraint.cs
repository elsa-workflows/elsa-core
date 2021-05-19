using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Server.Api.RouteConstraints
{
    /// <summary>
    /// In case the hosting app wishes to opt-out of API Versioning, we still need to handle the "apiVersion" constraint.
    /// </summary>
    public class CompatibilityApiVersionConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) => true;
    }
}