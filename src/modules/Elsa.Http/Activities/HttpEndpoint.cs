using System.Text.Json.Serialization;
using System.Xml;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http;

/// <summary>
/// Wait for an inbound HTTP request that matches the specified path and methods.
/// </summary>
[Activity("Elsa", "HTTP", "Wait for an inbound HTTP request that matches the specified path and methods.")]
public class HttpEndpoint : Trigger<HttpRequest>
{
    internal const string HttpContextInputKey = "HttpContext";
    internal const string RequestPathInputKey = "RequestPath";

    /// <inheritdoc />
    [JsonConstructor]
    public HttpEndpoint()
    {
    }

    /// <summary>
    /// The path to associate with the workflow.
    /// </summary>
    [Input(Description = "The path to associate with the workflow.")]
    public Input<string> Path { get; set; } = default!;

    /// <summary>
    /// The HTTP methods to accept.
    /// </summary>
    [Input(
        Description = "The HTTP methods to accept.",
        Options = new[] { "GET", "POST", "PUT", "HEAD", "DELETE" },
        UIHint = InputUIHints.CheckList)]
    public Input<ICollection<string>> SupportedMethods { get; set; } = new(JsonLiteral.From(new[] { HttpMethods.Get }));

    /// <summary>
    /// Allow authenticated requests only.
    /// </summary>
    [Input(Description = "Allow authenticated requests only.", Category = "Security")]
    public Input<bool> Authorize { get; set; } = new(false);

    /// <summary>
    /// Provide a policy to evaluate. If the policy fails, the request is forbidden.
    /// </summary>
    [Input(Description = "Provide a policy to evaluate. If the policy fails, the request is forbidden.", Category = "Security")]
    public Input<string?> Policy { get; set; } = new(default(string?));

    /// <summary>
    /// The parsed request content, if any.
    /// </summary>
    [Output(Description = "The parsed request content, if any.")]
    public Output<object?> ParsedContent { get; set; } = default!;

    /// <summary>
    /// The parsed route data, if any.
    /// </summary>
    [Output(Description = "The parsed route data, if any.")]
    public Output<Dictionary<string,object>> RouteData { get; set; } = default!;

    /// <summary>
    /// The querystring data, if any.
    /// </summary>
    [Output(Description = "The querystring data, if any.")]
    public Output<Dictionary<string, object>> QueryStringData { get; set; } = default!;

    /// <summary>
    /// The headers, if any.
    /// </summary>
    [Output(Description = "The headers, if any.")]
    public Output<Dictionary<string, object>> Headers { get; set; } = default!;

    /// <inheritdoc />
    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => GetBookmarkPayloads(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {

        // If we did not receive external input, it means we are just now encountering this activity and we need to block execution by creating a bookmark.
        if (!context.TryGetInput<bool>(HttpContextInputKey, out var isHttpContext))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmarks(GetBookmarkPayloads(context.ExpressionExecutionContext));
            return;
        }

        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (e.g. in a virtual actor).
            // Create a bookmark to allow the invoker to export the state and resume execution from there.

            context.CreateBookmark(OnResumeAsync);
            return;
        }

        await HandleRequestAsync(context, httpContext);
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're not in an HTTP context, so let's fail.
            throw new Exception("Cannot execute in a non-HTTP context");
        }

        await HandleRequestAsync(context, httpContext);
    }

    private async Task<object?> ParseContentAsync(ActivityExecutionContext context, HttpRequest httpRequest)
    {
        if (!HasContent(httpRequest))
            return null;

        var cancellationToken = context.CancellationToken;
        var targetType = ParsedContent.GetTargetType(context);
        var contentStream = httpRequest.Body;
        var contentType = httpRequest.ContentType;

        return await context.ParseContentAsync(contentStream, contentType, targetType, cancellationToken);
    }

    private static bool HasContent(HttpRequest httpRequest) => httpRequest.Headers.ContentLength > 0;

    private IEnumerable<object> GetBookmarkPayloads(ExpressionExecutionContext context)
    {
        // Generate bookmark data for path and selected methods.
        var path = context.Get(Path);
        var methods = context.Get(SupportedMethods);
        return methods!.Select(x => new HttpEndpointBookmarkPayload(path!, x.ToLowerInvariant())).Cast<object>().ToArray();
    }

    private async Task HandleRequestAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        // Provide the received HTTP request as output.
        var request = httpContext.Request;
        context.Set(Result, request);

        // Read route data, if any.
        var path = context.GetInput<PathString>(RequestPathInputKey);
        var routeData = GetRouteData(httpContext, path);

        var routeDictionary = new Dictionary<string, object>();
        foreach (var route in routeData.Values) {
            routeDictionary.Add(route.Key, route.Value);
        }

        var queryStringDictionary = new Dictionary<string, object>();
        foreach (var queryString in httpContext.Request.Query)
        {
            queryStringDictionary.Add(queryString.Key, queryString.Value[0]);
        }

        var headersDictionary = new Dictionary<string, object>();
        foreach (var header in httpContext.Request.Headers)
        {
            headersDictionary.Add(header.Key, header.Value[0]);
        }

        context.Set(RouteData, routeDictionary);
        context.Set(QueryStringData, queryStringDictionary);
        context.Set(Headers, headersDictionary);

        // Read content, if any.
        var content = await ParseContentAsync(context, request);
        context.Set(ParsedContent, content);

        // Complete.
        await context.CompleteActivityAsync();
    }

    private static RouteData GetRouteData(HttpContext httpContext, string path)
    {
        var routeData = httpContext.GetRouteData();
        var routeTable = httpContext.RequestServices.GetRequiredService<IRouteTable>();
        var routeMatcher = httpContext.RequestServices.GetRequiredService<IRouteMatcher>();

        var matchingRouteQuery =
            from route in routeTable
            let routeValues = routeMatcher.Match(route, path)
            where routeValues != null
            select new { route, routeValues };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();

        if (matchingRoute == null)
            return routeData;

        foreach (var (key, value) in matchingRoute.routeValues!)
            routeData.Values[key] = value;

        return routeData;
    }
}