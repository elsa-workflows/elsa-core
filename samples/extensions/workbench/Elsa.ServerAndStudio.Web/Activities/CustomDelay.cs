using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.ServerAndStudio.Web.Activities;

public class CustomDelay : Activity
{
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.DelayFor(TimeSpan.FromSeconds(5), FailAsync);
        context.DelayFor(TimeSpan.FromSeconds(500), OnDelayElapsedAsync);
        return default;
    }
    
    private ValueTask FailAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Failing!");
        throw new("Failing!");
    }

    private async ValueTask OnDelayElapsedAsync(ActivityExecutionContext context)
    {
        Console.WriteLine("Delay elapsed");
        await context.CompleteActivityAsync();
    }
}