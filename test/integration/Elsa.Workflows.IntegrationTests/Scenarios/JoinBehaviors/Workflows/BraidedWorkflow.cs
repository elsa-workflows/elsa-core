using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;

public class BraidedWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var writeLine1 = new WriteLine("WriteLine1");
        var writeLine2 = new WriteLine("WriteLine2");
        var writeLine3 = new WriteLine("WriteLine3");
        var writeLine4 = new WriteLine("WriteLine4");
        var writeLine5 = new WriteLine("WriteLine5");
        var writeLine6 = new WriteLine("WriteLine6");
        var writeLine7 = new WriteLine("WriteLine7");

        workflow.Root = new Flowchart
        {
            Start = writeLine1,
            
            Activities =
            {
                writeLine1,
                writeLine2,
                writeLine3,
                writeLine4,
                writeLine5,
                writeLine6,
                writeLine7,
            },

            Connections =
            {
                new(writeLine1, writeLine2),
                new(writeLine1, writeLine3),

                new(writeLine2, writeLine4),
                new(writeLine2, writeLine5),

                new(writeLine3, writeLine5),
                new(writeLine3, writeLine6),

                new(writeLine4, writeLine7),
                new(writeLine5, writeLine7),
                new(writeLine6, writeLine7),
            }
        };
    }
}