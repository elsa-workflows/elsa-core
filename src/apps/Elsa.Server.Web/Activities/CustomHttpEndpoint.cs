using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.Workflows;

namespace Elsa.Server.Web.Activities;

public class CustomHttpEndpoint : Trigger
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.WaitForHttpRequest("my-path", HttpMethods.Get, OnHttpRequestReceivedAsync);
    }

    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context)
    {
        context.TriggerName = HttpStimulusNames.HttpEndpoint;
        return context.GetHttpEndpointStimuli("my-path", HttpMethods.Get);
    }

    private async ValueTask OnHttpRequestReceivedAsync(ActivityExecutionContext context)
    {
        var httpContextAccessor = context.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext!;
        
        httpContext.Response.StatusCode = 200;
        await httpContext.Response.WriteAsync("Hello World", context.CancellationToken);
        await context.CompleteActivityAsync();
    }
}