// Taken & adapted from https://github.com/markvincze/Stubbery/blob/main/src/Stubbery/RequestMatching/RouteMatcher.cs

using Elsa.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class RouteMatcher : IRouteMatcher
{
    /// <inheritdoc />
    public RouteValueDictionary? Match(string routeTemplate, string route)
    {
        var normalizedRoute = route.NormalizeRoute();
        var normalizedRouteTemplate = routeTemplate.NormalizeRoute();
        var template = TemplateParser.Parse(normalizedRouteTemplate);
        var matcher = new TemplateMatcher(template, GetDefaults(template));
        var values = new RouteValueDictionary();

        return matcher.TryMatch(normalizedRoute, values) ? values : null;
    }

    private static RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
    {
        var result = new RouteValueDictionary();

        foreach (var parameter in parsedTemplate.Parameters)
            if (parameter.DefaultValue != null)
                result.Add(parameter.Name!, parameter.DefaultValue);

        return result;
    }
}