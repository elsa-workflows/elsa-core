using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

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
                new For(0, 3)
                {
                    CurrentValue = new Output<MemoryBlockReference?>(currentValue),
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