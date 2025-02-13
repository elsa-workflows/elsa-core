using Elsa.Workflows.Activities.Flowchart.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.FlowJoins;

public class SingleJoinWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Elsa.Workflows.Activities.Flowchart.Activities.Flowchart
        {
            Activities =
            {
                new FlowJoin()
            }
        };
    }
}