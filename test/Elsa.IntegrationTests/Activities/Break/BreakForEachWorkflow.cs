using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

class BreakForEachWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
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
                    CurrentValue = currentItem,
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new If(context => currentItem.Get(context) == "Rust")
                            {
                                Then = new Break()
                            },
                            new WriteLine(currentItem),
                            new WriteLine("Test")
                        }
                    }
                },
                new WriteLine("End"),
            }
        });
    }
}