using Elsa.IntegrationTests.Scenarios.FlowchartNextActivity.Activities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.FlowchartNextActivity.Workflows;

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