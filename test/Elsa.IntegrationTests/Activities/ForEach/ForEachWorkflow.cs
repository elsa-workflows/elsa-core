using System.Collections.Generic;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities;

class ForEachWorkflow : WorkflowBase
{
    private readonly ICollection<string> _items;

    public ForEachWorkflow(ICollection<string> items)
    {
        _items = items;
    }
        
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var currentItem = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new ForEach<string>
                {
                    Items = new Input<ICollection<string>>(_items),
                    CurrentValue = currentItem,
                    Body = new WriteLine(currentItem)
                },
            }
        });
    }
}