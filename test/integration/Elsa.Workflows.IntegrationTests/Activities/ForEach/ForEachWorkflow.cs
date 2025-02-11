using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

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
        var currentItem = new Variable<string>
        {
            Name = "CurrentItem"
        };

        workflow.Root = new Sequence
        {
            Variables = { currentItem },
            Activities =
            {
                new ForEach<string>
                {
                    Items = new Input<ICollection<string>>(_items),
                    CurrentValue = new Output<string?>(currentItem),
                    Body = new WriteLine(currentItem)
                },
            }
        };
    }
}