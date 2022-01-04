using System.Net;
using System.Threading.Tasks;
using Elsa.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http;

public class WriteHttpResponse : Activity
{
    public Input<HttpStatusCode> StatusCode { get; set; } = new(HttpStatusCode.OK);
    public Input<string?> Content { get; set; } = new("");

    public override async ValueTask ExecuteAsync(ActivityExecutionContext context)
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