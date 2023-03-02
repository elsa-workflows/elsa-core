using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

public class BreakWhileWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.Root = new Sequence
        {
            Variables = { currentValue },
            Activities =
            {
                new WriteLine("Start"),
                While.True(new Sequence
                {
                    Activities =
                    {
                        new SetVariable
                        {
                            Variable = currentValue,
                            Value = new Input<object?>(context => currentValue.Get(context) + 1)
                        },
                        new If(context => currentValue.Get(context) == 3)
                        {
                            Then = new Break()
                        },
                        new WriteLine(context => currentValue.Get(context).ToString()),
                    }
                }),
                new WriteLine("End"),
            }
        };
    }
}