using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Trimble.Elsa.Activities.Activities;

public class SetVariableExtended : SetVariable
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await base.ExecuteAsync(context);
    }
}
