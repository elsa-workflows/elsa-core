using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.IntegrationTests.Activities;

public class BreakWhileWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var currentValue = new Variable<int?>(0);

        workflow.WithRoot(new Sequence
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
        });
    }
}