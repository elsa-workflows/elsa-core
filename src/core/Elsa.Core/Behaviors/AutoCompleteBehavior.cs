using Elsa.Models;

namespace Elsa.Behaviors;

/// <summary>
/// Automatically completes the currently executing activity.
/// </summary>
public class AutoCompleteBehavior : Behavior
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityAsync();
    }
}