using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Server.Web.Activities;

public class CustomDelay : Activity
{
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.DelayFor(TimeSpan.FromSeconds(5), OnDelayElapsedAsync);
        return default;
    }

    private async ValueTask OnDelayElapsedAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Delay elapsed");
        await context.CompleteActivityAsync();
    }
}