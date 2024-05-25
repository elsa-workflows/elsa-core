using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.FlowJoins;

public class SingleJoinWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Flowchart
        {
            Activities =
            {
                new FlowJoin()
            }
        };
    }
}