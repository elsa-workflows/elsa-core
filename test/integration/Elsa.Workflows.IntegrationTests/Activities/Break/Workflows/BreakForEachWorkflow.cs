using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;

class BreakForEachWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var items = new[] { "C#", "Rust", "Go" };
        var currentItem = new Variable<string>();

        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new ForEach<string>
                {
                    Items = new Input<ICollection<string>>(items),
                    CurrentValue = new (currentItem),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new If(context => currentItem.Get(context) == "Rust")
                            {
                                Then = new Break()
                            },
                            new WriteLine(currentItem)
                        }
                    }
                },
                new WriteLine("End"),
            }
        };
    }
}