using System.Collections.Generic;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

class BreakForEachWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var items = new[] { "C#", "Rust", "Go" };
        var currentItem = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Start"),
                new ForEach<string>
                {
                    Items = new Input<ICollection<string>>(items),
                    CurrentValue = new Output<string?>(currentItem),
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
        });
    }
}