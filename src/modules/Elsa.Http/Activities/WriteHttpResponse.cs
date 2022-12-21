using System.Net;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;

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