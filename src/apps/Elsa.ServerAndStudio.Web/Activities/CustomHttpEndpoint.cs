using Elsa.Http;
using Elsa.Workflows;

namespace Elsa.ServerAndStudio.Web.Activities;

public class CustomHttpEndpoint : HttpEndpointBase
{
    protected override HttpEndpointOptions GetOptions()
    {
        return new()
        {
            Path = "my-path",
            Methods = [HttpMethods.Get],
        };
    }

    protected override async ValueTask OnHttpRequestReceivedAsync(ActivityExecutionContext context, HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 200;
        await httpContext.Response.WriteAsync("Hello World", context.CancellationToken);
    }
}