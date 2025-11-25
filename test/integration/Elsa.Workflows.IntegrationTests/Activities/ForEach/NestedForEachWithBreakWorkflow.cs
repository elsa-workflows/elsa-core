using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class NestedForEachWithBreakWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var outerItems = new[] { "C#", "Rust", "Go" };
        var innerItems = new[] { "Classes", "Functions", "Modules" };
        var currentOuterItem = new Variable<string>("CurrentOuterItem", "");
        var currentInnerItem = new Variable<string>("CurrentInnerItem", "");

        workflow.Root = new ForEach<string>(outerItems)
        {
            CurrentValue = new (currentOuterItem),
            Body = new Sequence
            {
                Activities =
                {
                    new WriteLine(currentOuterItem),
                    new ForEach<string>(innerItems)
                    {
                        CurrentValue = new (currentInnerItem),
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
        };
    }
}