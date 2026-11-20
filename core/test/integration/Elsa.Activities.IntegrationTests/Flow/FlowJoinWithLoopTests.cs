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
/// Every scenario runs until the loop waits, persists and restores the state, and then resumes the wait.
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

    private string Log(string entry)
    {
        _log.Add(entry);
        return entry;
    }

    private Inline Logger(string entry) => new(() => Log(entry));

    /// <summary>
    /// Runs the flowchart until <paramref name="wait"/> suspends it, persists and restores the state, and resumes <paramref name="wait"/>.
    /// </summary>
    private async Task<WorkflowState> RunAndResumeAsync(Flowchart flowchart, FlowchartExecutionMode executionMode, IActivity wait)
    {
        await _fixture.BuildAsync();
        var runner = _fixture.Services.GetRequiredService<IWorkflowRunner>();
        var serializer = _fixture.Services.GetRequiredService<IWorkflowStateSerializer>();
        var workflow = Workflow.FromActivity(flowchart);

        var suspended = await runner.RunAsync(workflow, new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode));
        var bookmark = Assert.Single(suspended.WorkflowState.Bookmarks, x => x.ActivityId == wait.Id);
        var restored = serializer.Deserialize(serializer.Serialize(suspended.WorkflowState));
        var resumeOptions = new RunWorkflowOptions { BookmarkId = bookmark.Id }.WithFlowchartExecutionMode(executionMode);

        return (await runner.RunAsync(workflow, restored, resumeOptions)).WorkflowState;
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
    /// Suspends until resumed.
    /// </summary>
    private sealed class WaitForResume : Activity
    {
        protected override void Execute(ActivityExecutionContext context) => context.CreateBookmark();
    }
}
