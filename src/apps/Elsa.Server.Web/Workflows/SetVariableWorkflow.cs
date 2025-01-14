using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Server.Web.Workflows;

public class SetVariableWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var myVar = builder.WithVariable<int>();
        builder.Root = new Sequence
        {
            Activities =
            [
                new SetVariable
                {
                    Variable = myVar,
                    Value = new(42)
                }
            ]
        };
    }
}