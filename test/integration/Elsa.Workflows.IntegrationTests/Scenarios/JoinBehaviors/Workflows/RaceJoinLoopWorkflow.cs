using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;

/// <summary>
/// Reproduces https://github.com/elsa-workflows/elsa-core/issues/7993:
/// a WaitAny (Race) join with two forward inbounds that is re-entered through a loop
/// must fire again when triggered from a different inbound than the previous activation.
/// </summary>
public class RaceJoinLoopWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var counter = builder.WithVariable(0);
        var start = new WriteLine("Start");
        var whichPath = new FlowDecision(context => counter.Get(context) == 0);
        var isSecondPass = new FlowDecision(context => counter.Get(context) == 2);
        var pathA = new WriteLine("A");
        var pathB = new WriteLine("B");
        var join = new FlowJoin(); // WaitAny by default → MergeMode.Race
        var body = new WriteLine("Body");
        var increment = new SetVariable<int>(counter, context => counter.Get(context) + 1);
        var loopDecision = new FlowDecision(context => counter.Get(context) <= 1);
        var retry = new WriteLine("Retry");
        var end = new WriteLine("End");

        builder.Root = new Flowchart
        {
            Start = start,
            Activities =
            {
                start, whichPath, isSecondPass, pathA, pathB, join, body, increment, loopDecision, retry, end
            },
            Connections =
            {
                new(start, whichPath),
                new(new(whichPath, "True"), new Endpoint(pathA)),
                new(new(whichPath, "False"), new Endpoint(isSecondPass)),
                new(new(isSecondPass, "True"), new Endpoint(pathB)),
                new(new(isSecondPass, "False"), new Endpoint(end)),
                // Two forward inbounds into the same WaitAny join.
                new(pathA, join),
                new(pathB, join),
                new(join, body),
                new(body, increment),
                new(increment, loopDecision),
                // Loop back-edge into the join.
                new(new(loopDecision, "True"), new Endpoint(retry)),
                new(retry, join),
                // Exit the loop and re-enter the join through the other forward inbound.
                new(new(loopDecision, "False"), new Endpoint(whichPath)),
            }
        };
    }
}
