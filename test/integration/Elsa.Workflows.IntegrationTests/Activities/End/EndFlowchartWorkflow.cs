using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Activities.Workflows;
public class EndFlowchartWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow) 
    {
        WriteLine start = new("start");
        WriteLine end = new("end");
        If condition = new(c => true)
        {
            Then = new End()
        };

        workflow.Root = new Flowchart()
        {
            Activities = {
                start, condition, end
            },
            Connections = {
                new Connection(start, condition),
                new Connection(condition, end),
            }
        };
    }
}
