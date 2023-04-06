using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.ImplicitJoins.Workflows;

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
                new Connection(writeLine1, writeLine2),
                new Connection(writeLine1, writeLine3),
                new Connection(writeLine1, writeLine4),

                new Connection(writeLine2, writeLine5),
                new Connection(writeLine3, writeLine5),

                new Connection(writeLine3, writeLine6),
                new Connection(writeLine4, writeLine6),

                new Connection(writeLine5, writeLine7),
                new Connection(writeLine6, writeLine7),
            }
        };
    }
}