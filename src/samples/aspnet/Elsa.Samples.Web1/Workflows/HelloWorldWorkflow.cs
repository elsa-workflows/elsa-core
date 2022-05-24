using Elsa.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class HelloWorldWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new WriteLine("Hello World!"));
    }
}