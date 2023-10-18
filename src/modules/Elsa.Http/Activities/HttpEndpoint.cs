using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Elsa.Http;

/// <summary>
/// Wait for an inbound HTTP request that matches the specified path and methods.
/// </summary>
[Activity("Elsa", "HTTP", "Wait for an inbound HTTP request that matches the specified path and methods.", DisplayName = "HTTP Endpoint")]
[Output(IsSerializable = false)]
public class HttpEndpoint : Trigger<HttpRequest>
{
    internal const string HttpContextInputKey = "HttpContext";
    internal const string RequestPathInputKey = "RequestPath";

    /// <inheritdoc />
    public HttpEndpoint([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
    /// The maximum time allowed to process the request.
    /// </summary>
    [Input(Description = "The maximum time allowed to process the request.", Category = "Upload")]
    public Input<TimeSpan?> RequestTimeout { get; set; } = default!;

    /// <summary>
    /// The maximum request size allowed in bytes.
    /// </summary>
    [Input(Description = "The maximum request size allowed in bytes.", Category = "Upload")]
    public Input<long?> RequestSizeLimit { get; set; } = default!;

    /// <summary>
    /// The maximum request size allowed in bytes.
    /// </summary>
    [Input(Description = "The maximum file size allowed in bytes for an individual file.", Category = "Upload")]
    public Input<long?> FileSizeLimit { get; set; } = default!;

    /// <summary>
    /// The allowed file extensions,
    /// </summary>
    [Input(Description = "Only file extensions in this list are allowed. Leave empty to allow all extensions", Category = "Upload", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> AllowedFileExtensions { get; set; } = default!;

    /// <summary>
    /// The allowed file extensions,
    /// </summary>
    [Input(Description = "File extensions in this list are forbidden. Leave empty to not block any extension.", Category = "Upload", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> BlockedFileExtensions { get; set; } = default!;

    /// <summary>
    /// The allowed file extensions,
    /// </summary>
    [Input(Description = "Only MIME types in this list are allowed. Leave empty to allow all types", Category = "Upload", UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>> AllowedMimeTypes { get; set; } = default!;

    /// <summary>
    /// A value indicating whether to expose the "Request too large" outcome.
    /// </summary>
    [Input(Description = "A value indicating whether to expose the \"Request too large\" outcome.", Category = "Outcomes")]
    public bool ExposeRequestTooLargeOutcome { get; set; }

    /// <summary>
    /// A value indicating whether to expose the "File too large" outcome.
    /// </summary>
    [Input(Description = "A value indicating whether to expose the \"File too large\" outcome.", Category = "Outcomes")]
    public bool ExposeFileTooLargeOutcome { get; set; }

    /// <summary>
    /// A value indicating whether to expose the "Invalid file extension" outcome.
    /// </summary>
    [Input(Description = "A value indicating whether to expose the \"Invalid file extension\" outcome.", Category = "Outcomes")]
    public bool ExposeInvalidFileExtensionOutcome { get; set; }

    /// <summary>
    /// A value indicating whether to expose the "Invalid file MIME type" outcome.
    /// </summary>
    [Input(Description = "A value indicating whether to expose the \"Invalid file MIME type\" outcome.", Category = "Outcomes")]
    public bool ExposeInvalidFileMimeTypeOutcome { get; set; }

    /// <summary>
    /// The parsed request content, if any.
    /// </summary>
    [Output(Description = "The parsed request content, if any.")]
    public Output<object?> ParsedContent { get; set; } = default!;

    /// <summary>
    /// The uploaded files, if any.
    /// </summary>
    [Output(Description = "The uploaded files, if any.", IsSerializable = false)]
    public Output<IFormFile[]> Files { get; set; } = default!;

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
        var path = Path.Get(context);

        if (path.Contains("//"))
            throw new RoutePatternException(path, "Path cannot contain double slashes (//)");

        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmarks(GetBookmarkPayloads(context.ExpressionExecutionContext), includeActivityInstanceId: false);
            return;
        }

        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (e.g. in a virtual actor).
            // Create a bookmark to allow the invoker to export the state and resume execution from there.
            context.CreateBookmark(OnResumeAsync, BookmarkMetadata.HttpCrossBoundary);
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

    private async Task HandleRequestAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        // Provide the received HTTP request as output.
        var request = httpContext.Request;
        context.Set(Result, request);

        // Read route data, if any.
        var path = context.GetWorkflowInput<PathString>(RequestPathInputKey);
        var routeData = GetRouteData(httpContext, path);

        var routeDictionary = routeData.Values.ToDictionary(route => route.Key, route => route.Value!);
        var queryStringDictionary = httpContext.Request.Query.ToDictionary<KeyValuePair<string, StringValues>, string, object>(queryString => queryString.Key, queryString => queryString.Value[0]!);
        var headersDictionary = httpContext.Request.Headers.ToDictionary<KeyValuePair<string, StringValues>, string, object>(header => header.Key, header => header.Value[0]!);

        context.Set(RouteData, routeDictionary);
        context.Set(QueryStringData, queryStringDictionary);
        context.Set(Headers, headersDictionary);
        
        // Validate request size.
        if (!ValidateRequestSize(context, httpContext))
        {
            await HandleRequestTooLargeAsync(context, httpContext);
            return;
        }

        // Read files, if any.
        var files = ReadFilesAsync(context, request);

        if (files.Any())
        {
            if (!ValidateFileSizes(context, httpContext, files))
            {
                await HandleFileSizeTooLargeAsync(context, httpContext);
                return;
            }

            if (!ValidateFileExtensionWhitelist(context, httpContext, files))
            {
                await HandleInvalidFileExtensionWhitelistAsync(context, httpContext);
                return;
            }

            if (!ValidateFileExtensionBlacklist(context, httpContext, files))
            {
                await HandleInvalidFileExtensionBlacklistAsync(context, httpContext);
                return;
            }

            if (!ValidateFileMimeTypes(context, httpContext, files))
            {
                await HandleInvalidFileMimeTypesAsync(context, httpContext);
                return;
            }

            Files.Set(context, files.ToArray());
        }

        // Read content, if any.
        var content = await ParseContentAsync(context, request);
        ParsedContent.Set(context, content);

        // Complete.
        await context.CompleteActivityAsync();
    }

    private IFormFileCollection ReadFilesAsync(ActivityExecutionContext context, HttpRequest request)
    {
        return request.HasFormContentType ? request.Form.Files : new FormFileCollection();
    }

    private bool ValidateRequestSize(ActivityExecutionContext context, HttpContext httpContext)
    {
        var requestSizeLimit = RequestSizeLimit.GetOrDefault(context);

        if (!requestSizeLimit.HasValue)
            return true;

        var requestSize = httpContext.Request.ContentLength ?? 0;
        return requestSize <= requestSizeLimit;
    }
    
    private async Task HandleRequestTooLargeAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        var exposeRequestTooLargeOutcome = ExposeRequestTooLargeOutcome;

        if (exposeRequestTooLargeOutcome)
        {
            await context.CompleteActivityWithOutcomesAsync("Request too large");
        }
        else
        {
            var response = httpContext.Response;
            response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await response.WriteAsJsonAsync(new
            {
                Message = $"The maximum request size allowed is {RequestSizeLimit.Get(context)} bytes."
            });
            await response.Body.FlushAsync();
        }
    }
    
    private bool ValidateFileSizes(ActivityExecutionContext context, HttpContext httpContext, IFormFileCollection files)
    {
        var fileSizeLimit = FileSizeLimit.GetOrDefault(context);

        if (!fileSizeLimit.HasValue)
            return true;

        if (!files.Any(file => file.Length > fileSizeLimit.Value))
            return true;

        return false;
    }

    private async Task HandleFileSizeTooLargeAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        var exposeFileTooLargeOutcome = ExposeFileTooLargeOutcome;

        if (exposeFileTooLargeOutcome)
        {
            await context.CompleteActivityWithOutcomesAsync("File too large");
        }
        else
        {
            var response = httpContext.Response;
            response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await response.WriteAsJsonAsync(new
            {
                Message = $"The maximum file size allowed is {FileSizeLimit.Get(context)} bytes."
            });
            await response.Body.FlushAsync();
        }
    }

    private bool ValidateFileExtensionWhitelist(ActivityExecutionContext context, HttpContext httpContext, IFormFileCollection files)
    {
        var allowedFileExtensions = AllowedFileExtensions.GetOrDefault(context);

        if (allowedFileExtensions == null || !allowedFileExtensions.Any())
            return true;

        if (files.All(file => allowedFileExtensions.Contains(System.IO.Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private async Task HandleInvalidFileExtensionWhitelistAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        if (ExposeInvalidFileExtensionOutcome)
        {
            await context.CompleteActivityWithOutcomesAsync("Invalid file extension");
            return;
        }

        var response = httpContext.Response;
        var allowedFileExtensions = AllowedFileExtensions.GetOrDefault(context)!;
        response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        await response.WriteAsJsonAsync(new
        {
            Message = $"Only the following file extensions are allowed: {string.Join(", ", allowedFileExtensions)}"
        });
        await response.Body.FlushAsync();
    }

    private bool ValidateFileExtensionBlacklist(ActivityExecutionContext context, HttpContext httpContext, IFormFileCollection files)
    {
        var blockedFileExtensions = BlockedFileExtensions.GetOrDefault(context);

        if (blockedFileExtensions == null || !blockedFileExtensions.Any())
            return true;

        if (!files.Any(file => blockedFileExtensions.Contains(System.IO.Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private async Task HandleInvalidFileExtensionBlacklistAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        if (ExposeInvalidFileExtensionOutcome)
        {
            await context.CompleteActivityWithOutcomesAsync("Invalid file extension");
            return;
        }
        
        var blockedFileExtensions = BlockedFileExtensions.GetOrDefault(context)!;
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        await response.WriteAsJsonAsync(new
        {
            Message = $"The following file extensions are not allowed: {string.Join(", ", blockedFileExtensions)}"
        });
        await response.Body.FlushAsync();
    }

    private bool ValidateFileMimeTypes(ActivityExecutionContext context, HttpContext httpContext, IFormFileCollection files)
    {
        var allowedMimeTypes = AllowedMimeTypes.GetOrDefault(context);

        if (allowedMimeTypes == null || !allowedMimeTypes.Any())
            return true;

        if (files.All(file => allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private async Task HandleInvalidFileMimeTypesAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        if(ExposeInvalidFileMimeTypeOutcome)
        {
            await context.CompleteActivityWithOutcomesAsync("Invalid file MIME type");
            return;
        }
        
        var allowedMimeTypes = AllowedMimeTypes.GetOrDefault(context)!;
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
        await response.WriteAsJsonAsync(new
        {
            Message = $"Only the following MIME types are allowed: {string.Join(", ", allowedMimeTypes)}"
        });
        await response.Body.FlushAsync();
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
        var methods = SupportedMethods.GetOrDefault(context) ?? new List<string> { HttpMethods.Get };
        var authorize = Authorize.GetOrDefault(context);
        var policy = Policy.GetOrDefault(context);
        var requestTimeout = RequestTimeout.GetOrDefault(context);
        var requestSizeLimit = RequestSizeLimit.GetOrDefault(context);

        return methods
            .Select(x => new HttpEndpointBookmarkPayload(path!, x.ToLowerInvariant(), authorize, policy, requestTimeout, requestSizeLimit))
            .Cast<object>()
            .ToArray();
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