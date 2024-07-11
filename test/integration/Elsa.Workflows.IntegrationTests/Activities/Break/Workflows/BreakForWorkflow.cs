using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;

class BreakForWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentValue = new Variable<int?>();

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new For(0, 3, 1)
                {
                    CurrentValue = new (currentValue),
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
        };
    }
}