using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// A join whose inbound branch contains a loop must wait until the loop is left (#8465).
/// Every scenario runs until a wait suspends the workflow, persists and restores the state, and resumes the wait until it no longer suspends.
/// </summary>
public class FlowJoinWithLoopTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    private readonly List<string> _log = [];

    [Theory]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAll)]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAllActive)]
    [InlineData(FlowchartExecutionMode.TokenBased, FlowJoinMode.WaitAll)]
    public async Task Join_RunsOnceAfterTheLoopExits(FlowchartExecutionMode executionMode, FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Review; Review False -> Rework (waits) -> Review (loop); Review True -> Join
        //   Start -> Other -> Join; Join -> Active
        var reviews = 0;
        var start = new Start();
        var review = new FlowDecision(_ => Log(++reviews > 1 ? "approved" : "rejected") == "approved");
        var rework = new WaitForResume();
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, review, rework, other, join, active },
            Connections =
            {
                new(start, review),
                new(new Endpoint(review, "False"), new Endpoint(rework)),
                new(rework, review),
                new(new Endpoint(review, "True"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, executionMode, rework);

        // Assert
        AssertActiveRanOnceAfter("approved");
        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_RunsOnceAfterTheLoopExitsFromADecisionInsideTheLoop(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Work -> Check; Check False -> Rework (waits) -> Work (loop); Check True -> Join
        //   Start -> Other -> Join; Join -> Active
        var checks = 0;
        var start = new Start();
        var work = Logger("work");
        var check = new FlowDecision(_ => Log(++checks > 1 ? "approved" : "rejected") == "approved");
        var rework = new WaitForResume();
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, work, check, rework, other, join, active },
            Connections =
            {
                new(start, work),
                new(work, check),
                new(new Endpoint(check, "False"), new Endpoint(rework)),
                new(rework, work),
                new(new Endpoint(check, "True"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, rework);

        // Assert
        AssertActiveRanOnceAfter("approved");
        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_IsDecidedWhenTheLoopIsLeftThroughAnotherExit(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Review; Review False -> Rework (waits) -> Retry; Retry True -> Review (loop); Retry False -> GiveUp
        //   Review True -> Join; Start -> Other -> Join; Join -> Active
        // Review always rejects and Retry gives up, so the loop is left without Review -> Join ever being followed.
        var start = new Start();
        var review = new FlowDecision(_ => Log("rejected") == "approved");
        var rework = new WaitForResume();
        var retry = new FlowDecision(_ => false);
        var giveUp = Logger("gave up");
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, review, rework, retry, giveUp, other, join, active },
            Connections =
            {
                new(start, review),
                new(new Endpoint(review, "False"), new Endpoint(rework)),
                new(rework, retry),
                new(new Endpoint(retry, "True"), new Endpoint(review)),
                new(new Endpoint(retry, "False"), new Endpoint(giveUp)),
                new(new Endpoint(review, "True"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, rework);

        // Assert
        // WaitAll is skipped because Review -> Join was never followed; WaitAllActive runs on the Other branch, but only once the loop is left.
        if (joinMode == FlowJoinMode.WaitAll)
            Assert.DoesNotContain("ACTIVE", _log);
        else
            AssertActiveRanOnceAfter("gave up");

        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_WithoutALoop_RunsOnceAfterBothBranches(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Wait -> Join; Start -> Other -> Join; Join -> Active
        var start = new Start();
        var wait = new WaitForResume();
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, wait, other, join, active },
            Connections =
            {
                new(start, wait),
                new(wait, join),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, wait);

        // Assert
        AssertActiveRanOnceAfter("other");
        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_WaitsForTheOuterLoopWhenAnExitLeavesNestedLoops(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Outer (waits) -> Abort; Abort False -> Again; Again True -> Abort (inner loop); Again False -> Repeat
        //   Repeat True -> Outer (outer loop); Repeat False -> Finish
        //   Abort True -> Join (leaves both loops); Start -> Other -> Join; Join -> Active
        // The inner loop runs twice in the first outer pass. The second outer pass aborts out of both loops.
        var outerPasses = 0;
        var innerPasses = 0;
        var start = new Start();
        var outer = new WaitForResume();
        var abort = new FlowDecision(_ => outerPasses > 0 && Log("abort") == "abort");
        var again = new FlowDecision(_ => ++innerPasses % 2 == 1);
        var repeat = new FlowDecision(_ => ++outerPasses < 2);
        var finish = Logger("finish");
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, outer, abort, again, repeat, finish, other, join, active },
            Connections =
            {
                new(start, outer),
                new(outer, abort),
                new(new Endpoint(abort, "False"), new Endpoint(again)),
                new(new Endpoint(again, "True"), new Endpoint(abort)),
                new(new Endpoint(again, "False"), new Endpoint(repeat)),
                new(new Endpoint(repeat, "True"), new Endpoint(outer)),
                new(new Endpoint(repeat, "False"), new Endpoint(finish)),
                new(new Endpoint(abort, "True"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, outer);

        // Assert
        AssertActiveRanOnceAfter("abort");
        Assert.DoesNotContain("finish", _log);
        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_IsDecidedWhenTheLoopIsLeftPartway(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Gate; Gate True -> Check; Check True -> Rework (waits) -> Gate (loop)
        //   Gate False -> Broke -> Join (breaks out before the loop end); Check False -> Join
        //   Start -> Other -> Join; Join -> Active
        // The second pass breaks out at Gate, so Check is skipped and its exit, deferred in the first pass, is decided only then.
        var gates = 0;
        var start = new Start();
        var gate = new FlowDecision(_ => ++gates < 2);
        var check = new FlowDecision(_ => true);
        var rework = new WaitForResume();
        var broke = Logger("broke");
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, gate, check, rework, broke, other, join, active },
            Connections =
            {
                new(start, gate),
                new(new Endpoint(gate, "True"), new Endpoint(check)),
                new(new Endpoint(check, "True"), new Endpoint(rework)),
                new(rework, gate),
                new(new Endpoint(gate, "False"), new Endpoint(broke)),
                new(broke, join),
                new(new Endpoint(check, "False"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, rework);

        // Assert
        // WaitAll is skipped because Check -> Join was never followed; WaitAllActive runs once, after the break.
        if (joinMode == FlowJoinMode.WaitAll)
            Assert.DoesNotContain("ACTIVE", _log);
        else
            AssertActiveRanOnceAfter("broke");

        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_RunsOnceAfterTwoLoopsThatShareAStart(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Head -> Check; Check True -> Again; Again True -> Head (inner loop)
        //   Check False -> Merge; Again False -> Merge; Merge (waits) -> Repeat; Repeat True -> Head (outer loop)
        //   Repeat False -> Join; Start -> Other -> Join; Join -> Active
        // Both backward connections go into Head. The inner loop runs twice in each of the two outer passes,
        // and its deferred exit Check -> Merge has to be decided again in the second outer pass.
        var innerPasses = 0;
        var outerPasses = 0;
        var start = new Start();
        var head = Logger("head");
        var check = new FlowDecision(_ => true);
        var again = new FlowDecision(_ => ++innerPasses % 2 == 1);
        var merge = new WaitForResume();
        var repeat = new FlowDecision(_ => Log(++outerPasses < 2 ? "repeat" : "done") == "repeat");
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, head, check, again, merge, repeat, other, join, active },
            Connections =
            {
                new(start, head),
                new(head, check),
                new(new Endpoint(check, "True"), new Endpoint(again)),
                new(new Endpoint(again, "True"), new Endpoint(head)),
                new(new Endpoint(check, "False"), new Endpoint(merge)),
                new(new Endpoint(again, "False"), new Endpoint(merge)),
                new(merge, repeat),
                new(new Endpoint(repeat, "True"), new Endpoint(head)),
                new(new Endpoint(repeat, "False"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, merge);

        // Assert
        Assert.Equal(4, innerPasses);
        AssertActiveRanOnceAfter("done");
        AssertFinished(state);
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAll)]
    [InlineData(FlowJoinMode.WaitAllActive)]
    public async Task Join_RunsOnceAfterSiblingLoopsThatShareAStart(FlowJoinMode joinMode)
    {
        // Arrange
        //   Start -> Head -> Body (waits) -> Route; Route A -> LoopA -> Head; Route B -> LoopB -> Head
        //   Route Done -> Join; Start -> Other -> Join; Join -> Active
        // The two loops share Head, Body and Route, and neither is inside the other. The first pass goes round loop A.
        // The second pass leaves loop A and goes round loop B through the shared body. The third pass leaves through Done.
        var routes = new Queue<string>(["A", "B", "Done"]);
        var start = new Start();
        var head = Logger("head");
        var body = new WaitForResume();
        var route = new Route(() => Log(routes.Dequeue()));
        var loopA = Logger("loop A");
        var loopB = Logger("loop B");
        var other = Logger("other");
        var join = new FlowJoin { Mode = new(joinMode) };
        var active = Logger("ACTIVE");
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, head, body, route, loopA, loopB, other, join, active },
            Connections =
            {
                new(start, head),
                new(head, body),
                new(body, route),
                new(new Endpoint(route, "A"), new Endpoint(loopA)),
                new(loopA, head),
                new(new Endpoint(route, "B"), new Endpoint(loopB)),
                new(loopB, head),
                new(new Endpoint(route, "Done"), new Endpoint(join)),
                new(start, other),
                new(other, join),
                new(join, active)
            }
        };

        // Act
        var state = await RunAndResumeAsync(flowchart, FlowchartExecutionMode.CounterBased, body);

        // Assert
        // Each loop went round once and the shared body ran in all three passes; the join ran once, only after the last pass.
        Assert.Equal(["A", "B", "Done"], _log.Where(x => x is "A" or "B" or "Done"));
        Assert.Single(_log, x => x == "loop A");
        Assert.Single(_log, x => x == "loop B");
        Assert.Equal(3, _log.Count(x => x == "head"));
        AssertActiveRanOnceAfter("Done");
        AssertFinished(state);
    }

    private string Log(string entry)
    {
        _log.Add(entry);
        return entry;
    }

    private Inline Logger(string entry) => new(() => Log(entry));

    /// <summary>
    /// Runs the flowchart until <paramref name="wait"/> suspends it. Then, for as long as <paramref name="wait"/> suspends it again,
    /// persists and restores the state and resumes <paramref name="wait"/>.
    /// </summary>
    private async Task<WorkflowState> RunAndResumeAsync(Flowchart flowchart, FlowchartExecutionMode executionMode, IActivity wait)
    {
        await _fixture.BuildAsync();
        var runner = _fixture.Services.GetRequiredService<IWorkflowRunner>();
        var serializer = _fixture.Services.GetRequiredService<IWorkflowStateSerializer>();
        var workflow = Workflow.FromActivity(flowchart);

        var state = (await runner.RunAsync(workflow, new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode))).WorkflowState;
        Assert.Contains(state.Bookmarks, x => x.ActivityId == wait.Id);

        for (var resumes = 0; state.Bookmarks.FirstOrDefault(x => x.ActivityId == wait.Id) is { } bookmark; resumes++)
        {
            Assert.True(resumes < 10, "The workflow kept suspending at the wait.");
            var restored = serializer.Deserialize(serializer.Serialize(state));
            var resumeOptions = new RunWorkflowOptions { BookmarkId = bookmark.Id }.WithFlowchartExecutionMode(executionMode);
            state = (await runner.RunAsync(workflow, restored, resumeOptions)).WorkflowState;
        }

        return state;
    }

    private void AssertActiveRanOnceAfter(string entry)
    {
        Assert.Contains(entry, _log);
        Assert.Single(_log, x => x == "ACTIVE");
        Assert.True(_log.IndexOf("ACTIVE") > _log.IndexOf(entry), $"Expected ACTIVE after '{entry}', but the log was [{string.Join(", ", _log)}].");
    }

    private static void AssertFinished(WorkflowState state)
    {
        Assert.Equal(WorkflowStatus.Finished, state.Status);
        Assert.Equal(WorkflowSubStatus.Finished, state.SubStatus);
    }

    /// <summary>
    /// Completes with the outcome that <paramref name="next"/> returns.
    /// </summary>
    private sealed class Route(Func<string> next) : Activity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync(next());
    }

    /// <summary>
    /// Suspends until resumed.
    /// </summary>
    private sealed class WaitForResume : Activity
    {
        protected override void Execute(ActivityExecutionContext context) => context.CreateBookmark();
    }
}
