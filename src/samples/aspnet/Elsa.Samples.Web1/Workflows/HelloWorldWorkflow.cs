using Elsa.Activities.Console;
using Elsa.Contracts;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class HelloWorldWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new WriteLine("Hello World!"));
    }
}