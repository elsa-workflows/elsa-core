using Elsa.Extensions;

namespace Elsa.Workflows.ComponentTests.Scenarios.Variables.Activities;

public class CountdownStep : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var counter = context.GetVariable<int>("Counter");
        context.SetVariable("Counter", counter - 1);
        context?.CreateBookmark();
    }
}