using Elsa.Samples.Web1.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class CompositeActivitiesWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var name = new Variable<string?>();

        workflow.WithRoot(new Sequence
        {
            Variables = { name },
            Activities =
            {
                new MyGreeterComposite().CaptureOutput(x => x.Name, name),
                new WriteLine(context => $"Captured name: {name.Get(context)}")
            }
        });
    }
}