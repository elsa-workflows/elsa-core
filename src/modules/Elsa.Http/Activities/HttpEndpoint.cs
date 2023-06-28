using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Elsa.Http;

/// <summary>
/// Wait for an inbound HTTP request that matches the specified path and methods.
/// </summary>
[Activity("Elsa", "HTTP", "Wait for an inbound HTTP request that matches the specified path and methods.", DisplayName = "HTTP Endpoint")]
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
    public Input<ICollection<string>> SupportedMethods { get; set; } = new(ObjectLiteral.From(new[] { HttpMethods.Get }));

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
    public Output<IDictionary<string, object>> RouteData { get; set; } = default!;

    /// <summary>
    /// The querystring data, if any.
    /// </summary>
    [Output(Description = "The querystring data, if any.")]
    public Output<IDictionary<string, object>> QueryStringData { get; set; } = default!;

    /// <summary>
    /// The headers, if any.
    /// </summary>
    [Output(Description = "The headers, if any.")]
    public Output<IDictionary<string, object>> Headers { get; set; } = default!;

    /// <inheritdoc />
    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => GetBookmarkPayloads(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (!context.IsTriggerOfWorkflow())
        {
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
        var contentType = httpRequest.ContentType!;

        return await context.ParseContentAsync(contentStream, contentType, targetType, cancellationToken);
    }

    private static bool HasContent(HttpRequest httpRequest) => httpRequest.Headers.ContentLength > 0;

    private IEnumerable<object> GetBookmarkPayloads(ExpressionExecutionContext context)
    {
        // Generate bookmark data for path and selected methods.
        var path = context.Get(Path);
        var methods = SupportedMethods.GetOrDefault(context) ?? new List<string>{ HttpMethods.Get };
        var authorize = Authorize.GetOrDefault(context);
        var policy = Policy.GetOrDefault(context);
        return methods!.Select(x =>
            new HttpEndpointBookmarkPayload(path!, x.ToLowerInvariant(), authorize, policy))
            .Cast<object>().ToArray();
    }

    private async Task HandleRequestAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        // Provide the received HTTP request as output.
        var request = httpContext.Request;
        context.Set(Result, request);

        // Read route data, if any.
        var path = context.GetInput<PathString>(RequestPathInputKey);
        var routeData = GetRouteData(httpContext, path);

        var routeDictionary = routeData.Values.ToDictionary(route => route.Key, route => route.Value!);
        var queryStringDictionary = httpContext.Request.Query.ToDictionary<KeyValuePair<string, StringValues>, string, object>(queryString => queryString.Key, queryString => queryString.Value[0]!);
        var headersDictionary = httpContext.Request.Headers.ToDictionary<KeyValuePair<string, StringValues>, string, object>(header => header.Key, header => header.Value[0]!);

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