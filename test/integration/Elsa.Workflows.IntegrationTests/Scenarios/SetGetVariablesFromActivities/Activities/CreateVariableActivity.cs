using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Activities;

public class CreateVariableActivity : CodeActivity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.SetVariable("Foo", "Bar");
    }
}