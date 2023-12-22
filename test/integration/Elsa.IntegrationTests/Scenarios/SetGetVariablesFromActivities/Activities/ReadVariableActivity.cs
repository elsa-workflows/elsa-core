using Elsa.Extensions;
using Elsa.Workflows.Core;

namespace Elsa.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Activities;

public class ReadVariableActivity : CodeActivity<string>
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var foo = context.GetVariable<string>("Foo");
        Result.Set(context, foo);
    }
}