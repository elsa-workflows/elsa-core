using Elsa.Workflows.Core;

namespace Elsa.WorkflowServer.Web;

/// <summary>
/// A stubborn activity that only works after executing it 3 times.
/// </summary>
public class StubbornActivity : CodeActivity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var executionCount = context.UpdateProperty<int>("ExecutionCount", x => x + 1);

        if (executionCount < 3)
            throw new Exception("Not yet!");
    }
}