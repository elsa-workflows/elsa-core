using System.Linq;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Automatically completes the currently executing activity.
/// </summary>
public class AutoCompleteBehavior : Behavior
{
    public AutoCompleteBehavior(IActivity owner) : base(owner)
    {
    }
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If the activity created any bookmarks, do not complete. 
        if (context.Bookmarks.Any(x => x.ActivityId == context.Activity.Id))
            return;
        
        await context.CompleteActivityAsync();
    }
}