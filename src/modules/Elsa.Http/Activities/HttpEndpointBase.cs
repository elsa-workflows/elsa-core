using Elsa.Http.Extensions;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http;

public abstract class HttpEndpointBase : HttpEndpointBase<object>;

public abstract class HttpEndpointBase<TResult> : Trigger<TResult>
{
    protected abstract HttpEndpointOptions GetOptions();

    protected virtual ValueTask OnHttpRequestReceivedAsync(ActivityExecutionContext activityExecutionContext, HttpContext httpContext)
    {
        OnHttpRequestReceived(activityExecutionContext, httpContext);
        return default;
    }

    protected virtual void OnHttpRequestReceived(ActivityExecutionContext activityExecutionContext, HttpContext httpContext)
    {
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
    var options = GetOptions();
    await context.WaitForHttpRequestAsync(options, HttpRequestReceivedAsync, Elsa.Http.HttpStimulusNames.HttpEndpoint);
    }

    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context)
    {
        var options = GetOptions();
        context.TriggerName = HttpStimulusNames.HttpEndpoint;
        return context.GetHttpEndpointStimuli(options);
    }

    private async ValueTask HttpRequestReceivedAsync(ActivityExecutionContext context)
    {
        var httpContext = context.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        await OnHttpRequestReceivedAsync(context, httpContext);
        await context.CompleteActivityAsync();
    }
}