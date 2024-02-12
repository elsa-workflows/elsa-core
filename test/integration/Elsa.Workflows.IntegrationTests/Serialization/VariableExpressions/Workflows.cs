using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableExpressions;

class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var variable1 = workflow.WithVariable<string>("Some Value");
        var writeLine1 = new WriteLine(variable1);

        workflow.Root = new Flowchart
        {
            Activities =
            {
                writeLine1
            },
            Start = writeLine1
        };
    }
}   