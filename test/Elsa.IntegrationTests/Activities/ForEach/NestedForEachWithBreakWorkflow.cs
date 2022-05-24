using Elsa.Activities;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

class NestedForEachWithBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var outerItems = new[] { "C#", "Rust", "Go" };
        var innerItems = new[] { "Classes", "Functions", "Modules" };
        var currentOuterItem = new Variable<string>();
        var currentInnerItem = new Variable<string>();

        workflow.WithRoot(new ForEach<string>(outerItems)
        {
            CurrentValue = currentOuterItem,
            Body = new Sequence
            {
                Activities =
                {
                    new WriteLine(currentOuterItem),
                    new ForEach<string>(innerItems)
                    {
                        CurrentValue = currentInnerItem,
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new If(context => currentInnerItem.Get(context) == "Functions")
                                {
                                    Then = new Break()
                                },
                                new WriteLine(currentInnerItem)
                            }
                        }
                    }
                }
            }
        });
    }
}