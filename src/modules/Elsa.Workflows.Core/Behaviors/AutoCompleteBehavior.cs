using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Automatically completes the currently executing activity.
/// </summary>
public class AutoCompleteBehavior : Behavior
{
    /// <inheritdoc />
    public AutoCompleteBehavior(IActivity owner) : base(owner)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If the activity created any bookmarks, do not complete. 
        if (context.Bookmarks.Any(x => x.ActivityNodeId == context.ActivityNode.NodeId))
            return;
        
        await context.CompleteActivityAsync();
    }
}