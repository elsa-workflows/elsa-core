using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities;

class ForEachWorkflow(ICollection<string> items) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentItem = new Variable<string>("CurrentItem", "");

        workflow.Root = new Sequence
        {
            Variables = { currentItem },
            Activities =
            {
                new ForEach<string>
                {
                    Items = new(items),
                    CurrentValue = new(currentItem),
                    Body = new WriteLine(currentItem)
                },
            }
        };
    }
}