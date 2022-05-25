using System.Net;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http;

[Activity("Elsa", "HTTP", "Write an HTTP response.")]
public class WriteHttpResponse : Activity
{
    public Input<HttpStatusCode> StatusCode { get; set; } = new(HttpStatusCode.OK);
    public Input<string?> Content { get; set; } = new("");

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;
        var response = httpContext.Response;

        response.StatusCode = (int)context.Get(StatusCode);

        var content = context.Get(Content);

        if (content != null)
            await response.WriteAsync(content, context.CancellationToken);
    }
}