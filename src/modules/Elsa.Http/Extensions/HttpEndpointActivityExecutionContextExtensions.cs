using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Elsa.Http.Extensions;

public static class HttpEndpointActivityExecutionContextExtensions
{
    public static void WaitForHttpRequest(this ActivityExecutionContext context, string path, string method, ExecuteActivityDelegate? callback = null)
    {
        var options = new HttpEndpointOptions
        {
            Path = path,
            Methods = [method]
        };
        WaitForHttpRequest(context, options, callback);
    }

    public static void WaitForHttpRequest(this ActivityExecutionContext context, string path, IEnumerable<string> methods, ExecuteActivityDelegate? callback = null)
    {
        var options = new HttpEndpointOptions
        {
            Path = path,
            Methods = methods.ToList()
        };
        WaitForHttpRequest(context, options, callback);
    }

    public static void WaitForHttpRequest(this ActivityExecutionContext context, HttpEndpointOptions options, ExecuteActivityDelegate? callback = null)
    {
        var path = options.Path;
        if (path.Contains("//"))
            throw new RoutePatternException(path, "Path cannot contain double slashes (//)");

        var expressionExecutionContext = context.ExpressionExecutionContext;
        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmarks(expressionExecutionContext.GetHttpEndpointStimuli(options), includeActivityInstanceId: false, callback: callback);
            return;
        }

        callback?.Invoke(context);
    }

    public static IEnumerable<object> GetHttpEndpointStimuli(this TriggerIndexingContext context, string path, string method)
    {
        return context.GetHttpEndpointStimuli(path, [method]);
    }
    
    public static IEnumerable<object> GetHttpEndpointStimuli(this TriggerIndexingContext context, string path, IEnumerable<string> methods)
    {
        var options = new HttpEndpointOptions
        {
            Path = path,
            Methods = methods.ToList()
        };

        return context.GetHttpEndpointStimuli(options);
    }

    public static IEnumerable<object> GetHttpEndpointStimuli(this TriggerIndexingContext context, HttpEndpointOptions options)
    {
        return context.ExpressionExecutionContext.GetHttpEndpointStimuli(options);
    }

    public static IEnumerable<object> GetHttpEndpointStimuli(this ExpressionExecutionContext context, HttpEndpointOptions options)
    {
        // Generate bookmark data for path and selected methods.
        var normalizedRoute = options.Path.NormalizeRoute();
        var authorize = options.Authorize;
        var policy = options.Policy;
        var requestTimeout = options.RequestTimeout;
        var requestSizeLimit = options.RequestSizeLimit;

        return options.Methods
            .Select(x => new HttpEndpointBookmarkPayload(normalizedRoute, x.ToLowerInvariant(), authorize, policy, requestTimeout, requestSizeLimit))
            .Cast<object>()
            .ToArray();
    }

    internal static void CreateCrossBoundaryBookmark(this ActivityExecutionContext context, ExecuteActivityDelegate? callback = null)
    {
        var bookmarkOptions = new CreateBookmarkArgs
        {
            BookmarkName = HttpStimulusNames.HttpEndpoint,
            Callback = callback,
            Metadata = BookmarkMetadata.HttpCrossBoundary,
        };
        context.CreateBookmark(bookmarkOptions);
    }
}

public class HttpEndpointOptions
{
    public string Path { get; set; } = null!;
    public ICollection<string> Methods { get; set; } = [HttpMethods.Get];
    public bool Authorize { get; set; }
    public string? Policy { get; set; }
    public TimeSpan? RequestTimeout { get; set; }
    public long? RequestSizeLimit { get; set; }
}