using Elsa.Extensions;

namespace Elsa.Workflows.ComponentTests.Scenarios.VariablesArray.Activities;

public class RemoveTopElementStep : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var elements = context.GetVariable<string[]>("Elements");
        context.SetVariable("Elements", elements!.Skip(1).ToArray());
        context?.CreateBookmark();
    }
}