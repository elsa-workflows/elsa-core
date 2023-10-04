using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.IntegrationTests.Activities.Workflows;

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