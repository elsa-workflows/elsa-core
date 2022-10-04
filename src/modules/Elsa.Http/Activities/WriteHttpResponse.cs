using System;
using System.Net;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http;

[Activity("Elsa", "HTTP", "Write an HTTP response.", DisplayName = "HTTP Response")]
public class WriteHttpResponse : Activity
{
    public Input<HttpStatusCode> StatusCode { get; set; } = new(HttpStatusCode.OK);
    public Input<string?> Content { get; set; } = new("");

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            // We're executing in a non-HTTP context (like in a virtual actor).
            // Create a bookmark to allow the invoker to get the export state and resume execution from there.

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