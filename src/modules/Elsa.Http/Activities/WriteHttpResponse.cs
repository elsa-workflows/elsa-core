using System.Net;
using System.Net.Http.Headers;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Microsoft.AspNetCore.Http;
using HttpRequestHeaders = Elsa.Http.Models.HttpRequestHeaders;
using HttpResponseHeaders = Elsa.Http.Models.HttpResponseHeaders;

namespace Elsa.Http;

/// <summary>
/// Write a response to the current HTTP response object.
/// </summary>
[Activity("Elsa", "HTTP", "Write a response to the current HTTP response object.", DisplayName = "HTTP Response")]
public class WriteHttpResponse : Activity
{
    /// <summary>
    /// The status code to return.
    /// </summary>
    [Input(DefaultValue = HttpStatusCode.OK, Description = "The status code to return.")]
    public Input<HttpStatusCode> StatusCode { get; set; } = new(HttpStatusCode.OK);

    /// <summary>
    /// The content to write back.
    /// </summary>
    [Input(Description = "The content to write back.")]
    public Input<string?> Content { get; set; } = new("");

    /// <summary>
    /// The content type to use when returning the response.
    /// </summary>
    [Input(
        Description = "The content type to use when returning the response.",
        Options = new[] { "", "text/plain", "text/html", "application/json", "application/xml", "application/x-www-form-urlencoded" },
        UIHint = InputUIHints.Dropdown
    )]
    public Input<string?> ContentType { get; set; } = default!;
    
    /// <summary>
    /// The headers to return along with the response.
    /// </summary>
    [Input(Description = "The headers to return along with the response.", Category = "Advanced")]
    public Input<HttpResponseHeaders?> ResponseHeaders { get; set; } = new(new HttpResponseHeaders());

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (e.g. in a virtual actor).
            // Create a bookmark to allow the invoker to export the state and resume execution from there.

            context.CreateBookmark(OnResumeAsync);
            return;
        }

        var response = httpContext.Response;
        response.StatusCode = (int)context.Get(StatusCode);
        
        var headers = ResponseHeaders.TryGet(context) ?? new HttpResponseHeaders();
        foreach (var header in headers)
            response.Headers.Add(header.Key, header.Value);

        response.ContentType = ContentType.TryGet(context);
        var content = context.Get(Content);
        if (content != null)
            await response.WriteAsync(content, context.CancellationToken);
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

        var response = httpContext.Response;

        response.StatusCode = (int)context.Get(StatusCode);

        var content = context.Get(Content);

        if (content != null)
            await response.WriteAsync(content, context.CancellationToken);
    }
}