using Elsa.Models;

namespace Elsa.Behaviors;

/// <summary>
/// Automatically completes the currently executing activity.
/// </summary>
public class AutoCompleteBehavior : Behavior
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If the activity created any bookmarks, do not complete. 
        if (context.Bookmarks.Any(x => x.ActivityId == context.Activity.Id))
            return;
        
        await context.CompleteActivityAsync();
    }
}