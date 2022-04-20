using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;

namespace Elsa.IntegrationTests.Activities;

class BreakForWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var currentValue = new Variable<int?>();

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new For(0, 3)
                {
                    CurrentValue = currentValue,
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new If(context => currentValue.Get(context) == 2)
                            {
                                Then = new Break()
                            },
                            new WriteLine(context => currentValue.Get(context).ToString()),
                        }
                    }
                },
                new WriteLine("End"),
            }
        });
    }
}