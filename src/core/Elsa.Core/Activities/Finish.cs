using Elsa.Attributes;
using Elsa.Behaviors;
using Elsa.Models;

namespace Elsa.Activities;

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
        context.WorkflowExecutionContext.ClearBookmarks();
        context.WorkflowExecutionContext.TransitionTo(WorkflowSubStatus.Finished);
    }
}