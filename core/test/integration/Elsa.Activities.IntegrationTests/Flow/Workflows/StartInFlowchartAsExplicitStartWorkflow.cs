using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Activities.IntegrationTests.Flow.Workflows;

/// <summary>
/// Workflow demonstrating that an explicit Start activity in a flowchart is used as the starting point.
/// </summary>
public class StartInFlowchartAsExplicitStartWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);

        var startActivity = new Start();
        var startMessage = new WriteLine("Start activity");
        var afterStart = new WriteLine("After Start");
        var shouldNotExecute = new WriteLine("Should not execute");

        workflow.Root = new Flowchart
        {
            Activities =
            {
                startActivity,
                startMessage,
                afterStart,
                shouldNotExecute
            },
            Connections =
            {
                new Connection(startActivity, startMessage),
                new Connection(startMessage, afterStart)
            }
        };
    }
}
