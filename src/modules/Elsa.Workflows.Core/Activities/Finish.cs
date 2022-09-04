using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Mark the workflow as Finished")]
public class Finish : Activity
{
    public Finish()
    {
        // Don't let ancestor activities schedule additional work.
        Behaviors.Remove<AutoCompleteBehavior>();
    }
    
    protected override void Execute(ActivityExecutionContext context)
    {
        context.ClearCompletionCallbacks();
        context.WorkflowExecutionContext.Scheduler.Clear();
        context.WorkflowExecutionContext.Bookmarks.Clear();
        context.WorkflowExecutionContext.TransitionTo(WorkflowSubStatus.Finished);
    }
}