using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;

namespace Elsa.Server.Web.Activities;

[Activity("Elsa", "Authorization", "Authorizes a flow based on the configured policies.")]
[FlowNode("Authorized", "Unauthorized", "Error")]
public class AuthorizeFlow : Activity<string>
{
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpContext = context.GetRequiredService<IHttpContextAccessor>().HttpContext;

        if (httpContext == null)
            throw new InvalidOperationException("HttpContext is not available. Ensure that the activity is executed within an HTTP request context.");

        var bookmark = context.CreateBookmark(new AuthorizeStimulus(), OnResumeAsync);
        var redirectUrl = context.ExpressionExecutionContext.GenerateBookmarkTriggerUrl(bookmark.Id);

        Result.Set(context, redirectUrl);
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        if(!context.TryGetWorkflowInput<string>("Answer", out var response))
        {
            await context.CompleteActivityWithOutcomesAsync("Unauthorized");
            return;
        }

        switch (response)
        {
            case "Authorized":
                await context.CompleteActivityWithOutcomesAsync("Authorized");
                return;
            case "Error":
                await context.CompleteActivityWithOutcomesAsync("Error");
                return;
            default:
                await context.CompleteActivityWithOutcomesAsync("Unauthorized");
                break;
        }
    }
}

public record AuthorizeStimulus;