using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Contracts;

namespaces Elsa.Workflows.IntegrationTests.Activities.Workflows;
public class BreakWhileFlowchart : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        WtiteLine start = new("start");
        WriteLine end = new("end");
        If condition = new If(c => true) 
        {
            Then = new Break()
        };

        workflow.Root = While.True(new Flowchart()
        {
            Activities = 
            {
                start, condition, end
            },
            Connections = 
            {
                new Connection(start, condition),
                new Connection(condition, end),
            }
        });
    }
}
