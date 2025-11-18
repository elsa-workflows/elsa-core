using System.Net;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.UIHints;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Elsa.Http.Options;

namespace Elsa.Http;

/// <summary>
/// Write a response to the current HTTP response object.
/// </summary>
[Activity("Elsa", "HTTP", "Write a response to the current HTTP response object.", DisplayName = "HTTP Response")]
public class WriteHttpResponse : Activity
{
    /// <inheritdoc />
    public WriteHttpResponse([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    public WriteHttpResponse(Input<object?> content, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        Content = content;
    }
    
    /// <inheritdoc />
    public WriteHttpResponse(Input<object?> content, Input<string?> contentType, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        Content = content;
        ContentType = contentType;
    }

    /// <summary>
    /// The status code to return.
    /// </summary>
    [Input(
        DefaultValue = HttpStatusCode.OK,
        Description = "The status code to return.",
        UIHint = InputUIHints.DropDown
    )]
    public Input<HttpStatusCode> StatusCode { get; set; } = new(HttpStatusCode.OK);

    /// <summary>
    /// The content to write back.
    /// </summary>
    [Input(Description = "The content to write back. String values will be sent as-is, while objects will be serialized to a JSON string. Byte arrays and streams will be sent as files.")]
    public Input<object?> Content { get; set; } = null!;

    /// <summary>
    /// The content type to use when returning the response.
    /// </summary>
    [Input(
        Description = "The content type to write when sending the response.",
        UIHandler = typeof(HttpContentTypeOptionsProvider),
        UIHint = InputUIHints.DropDown
    )]
    public Input<string?> ContentType { get; set; } = null!;

    /// <summary>
    /// The headers to return along with the response.
    /// </summary>
    [Input(
        Description = "The headers to send along with the response.",
        UIHint = InputUIHints.JsonEditor,
        Category = "Advanced"
    )]
    public Input<HttpHeaders?> ResponseHeaders { get; set; } = new(new HttpHeaders());

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (e.g. in a virtual actor).
            // Create a bookmark to allow the invoker to export the state and resume execution from there.
            context.CreateBookmark(OnResumeAsync, BookmarkMetadata.HttpCrossBoundary);
            return;
        }

        await WriteResponseAsync(context, httpContext.Response);
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're not in an HTTP context, so let's fail.
            throw new FaultException(HttpFaultCodes.NoHttpContext, HttpFaultCategories.Http, DefaultFaultTypes.System, "Cannot execute in a non-HTTP context");
        }

        await WriteResponseAsync(context, httpContext.Response);
    }

    private async Task WriteResponseAsync(ActivityExecutionContext context, HttpResponse response)
    {
        // Set status code.
        var statusCode = StatusCode.GetOrDefault(context, () => HttpStatusCode.OK);
        response.StatusCode = (int)statusCode;

        // Add headers.
        var headers = context.GetHeaders(ResponseHeaders);
        foreach (var header in headers)
            response.Headers[header.Key] = header.Value;

        // Get content and content type.
        var content = context.Get(Content);

        if (content != null)
        {
            var contentType = ContentType.GetOrDefault(context);

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = DetermineContentType(content);

            var factories = context.GetServices<IHttpContentFactory>();
            var factory = factories.FirstOrDefault(httpContentFactory => httpContentFactory.SupportedContentTypes.Any(c => c == contentType)) ?? new TextContentFactory();
            var httpContent = factory.CreateHttpContent(content, contentType);

            // Set content type.
            response.ContentType = httpContent.Headers.ContentType?.ToString() ?? contentType;

            // Write content.
            if (statusCode != HttpStatusCode.NoContent)
            {
                try
                {
                    await httpContent.CopyToAsync(response.Body);
                }
                catch (NotSupportedException)
                {
                    // This can happen the Content property is a type that cannot be serialized or contains a type that cannot be serialized. 
                    await response.WriteAsync("The response includes a type that cannot be serialized.");
                }
            }
        }

        // Check if the configuration is set to flush immediatly the response to the caller.
        var options = context.GetRequiredService<IOptions<HttpActivityOptions>>();
        if (options.Value.WriteHttpResponseSynchronously)
            await response.CompleteAsync();

        // Complete activity.
        await context.CompleteActivityAsync();
    }

    private string DetermineContentType(object? content) => content is byte[] or Stream
        ? "application/octet-stream"
        : content is string
            ? "text/plain"
            : "application/json";
}
