using System.Collections.Generic;
using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Services;

namespace Elsa.IntegrationTests.Activities;

class ForEachWorkflow : IWorkflow
{
    private readonly ICollection<string> _items;

    public ForEachWorkflow(ICollection<string> items)
    {
        _items = items;
    }
        
    public void Build(IWorkflowDefinitionBuilder workflow)
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