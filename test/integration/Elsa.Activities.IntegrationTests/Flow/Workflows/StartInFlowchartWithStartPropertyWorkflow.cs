using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Activities.IntegrationTests.Flow.Workflows;

/// <summary>
/// Workflow demonstrating that the Flowchart.Start property takes precedence over an explicit Start activity.
/// </summary>
public class StartInFlowchartWithStartPropertyWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);

        var startActivity = new Start();
        var startActivityMessage = new WriteLine("Start activity");
        var startPropertyActivity = new WriteLine("Start property used");
        var afterStart = new WriteLine("After property start");

        workflow.Root = new Flowchart
        {
            Start = startPropertyActivity, // This takes precedence
            Activities =
            {
                startActivity, // This should be ignored
                startActivityMessage,
                startPropertyActivity,
                afterStart
            },
            Connections =
            {
                new Connection(startActivity, startActivityMessage),
                new Connection(startPropertyActivity, afterStart)
            }
        };
    }
}
