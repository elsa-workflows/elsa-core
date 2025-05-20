using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Activities;

class ForEachWorkflow : WorkflowBase
{
    private readonly ICollection<string> _items;

    public ForEachWorkflow(ICollection<string> items)
    {
        _items = items;
    }

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
                    Items = new(_items),
                    CurrentValue = new(currentItem),
                    Body = new WriteLine(currentItem)
                },
            }
        };
    }
}