using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities;

[Activity("Elsa", "Control Flow", "Mark the workflow as Finished")]
public class Finish : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.ClearCompletionCallbacks();
        context.WorkflowExecutionContext.ClearBookmarks();
        context.WorkflowExecutionContext.TransitionTo(WorkflowStatus.Finished);
    }
}