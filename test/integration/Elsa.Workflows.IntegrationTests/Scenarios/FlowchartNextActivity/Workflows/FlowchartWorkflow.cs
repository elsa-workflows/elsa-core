using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FlowchartNextActivity.Workflows;

public class FlowchartWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var customActivity = new CustomActivity();
        var writeLine1 = new WriteLine("Line 1");
        var writeLine2 = new WriteLine("Line 2");
        
        builder.Root = new Flowchart
        {
            Activities =
            {
                customActivity,
                writeLine1,
                writeLine2
            },
            Connections =
            {
                new Connection(new Endpoint(customActivity, "Done"), new Endpoint(writeLine1)),
                new Connection(new Endpoint(customActivity, "Fake"), new Endpoint(writeLine2)),
            }
        };
    }
}