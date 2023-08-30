using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.IntegrationTests.Activities.Workflows;

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
                            Value = new (context => currentValue.Get(context) + 1)
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