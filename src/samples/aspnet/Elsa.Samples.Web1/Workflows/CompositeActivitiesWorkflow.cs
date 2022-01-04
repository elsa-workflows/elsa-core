using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Samples.Web1.Activities;

namespace Elsa.Samples.Web1.Workflows;

public class CompositeActivitiesWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var name = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Variables = { name },
            Activities =
            {
                new MyGreeterComposite
                {
                    Name = new Output<string?>(name)
                },
                new WriteLine(context => $"Captured name: {name.Get(context)}")
            }
        });
    }
}