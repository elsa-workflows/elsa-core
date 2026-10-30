using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;

/// <summary>
/// Reproduces https://github.com/elsa-workflows/elsa-core/issues/8464:
/// in token mode, a WaitAny (Race) join must not complete the flowchart before it runs,
/// so an activity after the join can still create a bookmark.
/// </summary>
public class RaceJoinBookmarkWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var start = new WriteLine("Start");
        var pay = new Event("Pay") { Id = "Pay" };
        var remind = new Event("Remind") { Id = "Remind" };
        var join = new FlowJoin(); // WaitAny by default → MergeMode.Race
        var after = new Event("After") { Id = "After" };
        var end = new WriteLine("End");

        builder.Root = new Flowchart
        {
            Start = start,
            Activities = { start, pay, remind, join, after, end },
            Connections =
            {
                new(start, pay),
                new(start, remind),
                new(pay, join),
                new(remind, join),
                new(join, after),
                new(after, end),
            }
        };
    }
}

/// <summary>
/// The loop variant of <see cref="RaceJoinBookmarkWorkflow"/>: after the WaitAny join, the flow loops back
/// and re-opens both waits ("wait for payment, or send a reminder and wait again").
/// </summary>
public class RaceJoinBookmarkLoopWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var passes = builder.WithVariable(0);
        var start = new WriteLine("Start");
        var check = new FlowDecision(context => passes.Get(context) >= 2);
        var pay = new Event("Pay") { Id = "Pay" };
        var remind = new Event("Remind") { Id = "Remind" };
        var join = new FlowJoin(); // WaitAny by default → MergeMode.Race
        var increment = new SetVariable<int>(passes, context => passes.Get(context) + 1);
        var end = new WriteLine("End");

        builder.Root = new Flowchart
        {
            Start = start,
            Activities = { start, check, pay, remind, join, increment, end },
            Connections =
            {
                new(start, check),
                new(new(check, "False"), new Endpoint(pay)),
                new(new(check, "False"), new Endpoint(remind)),
                new(pay, join),
                new(remind, join),
                new(join, increment),
                // Loop back-edge: re-open both waits.
                new(increment, check),
                new(new(check, "True"), new Endpoint(end)),
            }
        };
    }
}
