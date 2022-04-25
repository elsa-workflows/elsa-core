using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Samples.Web1.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class CompositeActivitiesWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var name = new Variable<string?>();

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