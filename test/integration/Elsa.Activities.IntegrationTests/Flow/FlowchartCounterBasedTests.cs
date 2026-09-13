using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using static Elsa.Activities.IntegrationTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for counter-based flowchart execution strategy.
/// </summary>
public class FlowchartCounterBasedTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output;

    public FlowchartCounterBasedTests()
    {
        _output = new();
        _services = CreateServiceProvider(TestContext.Current!.Output.StandardOutput, _output);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeServiceProviderAsync(_services);
        }
        finally
        {
            _output.Dispose();
        }
    }

    [Test]
    [DisplayName("Executes simple linear flowchart")]
    public async Task ExecutesSimpleLinearFlowchart()
    {
        // Arrange
        var flowchart = CreateSimpleLinearFlowchart(
            new WriteLine("First"),
            new WriteLine("Second"),
            new WriteLine("Third")
        );

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines.Count).IsEqualTo(3);

        await Assert.That(_output.Lines.ElementAt(0)).IsEqualTo("First");

        await Assert.That(_output.Lines.ElementAt(1)).IsEqualTo("Second");

        await Assert.That(_output.Lines.ElementAt(2)).IsEqualTo("Third");

    }

    [Test]
    [DisplayName("Executes both branches in parallel flowchart")]
    public async Task ExecutesBothBranches()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var flowchart = CreateBranchingFlowchart(start, branch1, branch2);

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines.Count).IsEqualTo(3);

        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("Branch1");

        await Assert.That(_output.Lines).Contains("Branch2");

    }

    [Test]
    [DisplayName("Handles flowchart with no connections")]
    public async Task HandlesNoConnections()
    {
        // Arrange
        var activity = new WriteLine("Isolated");
        var flowchart = new Flowchart
        {
            Start = activity,
            Activities = { activity }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).HasSingleItem();

        await Assert.That(_output.Lines.ElementAt(0)).IsEqualTo("Isolated");

    }

    [Test]
    [DisplayName("Completes when start activity is null")]
    public async Task CompletesWhenStartIsNull()
    {
        // Arrange
        var flowchart = new Flowchart
        {
            Start = null
        };

        // Act
        var result = await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(result).IsNotNull();

        await Assert.That(_output.Lines).IsEmpty();

    }

    [Test]
    [DisplayName("Follows conditional branches with If activity")]
    public async Task FollowsConditionalBranches()
    {
        // Arrange
        var ifActivity = new If
        {
            Condition = new(true),
            Then = new WriteLine("Then branch"),
            Else = new WriteLine("Else branch")
        };
        var flowchart = new Flowchart
        {
            Start = ifActivity,
            Activities = { ifActivity }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).HasSingleItem();

        await Assert.That(_output.Lines.ElementAt(0)).IsEqualTo("Then branch");

    }

    [Test]
    [DisplayName("Executes join node with WaitAny mode")]
    public async Task ExecutesJoinNodeWaitAny()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var join = new FlowJoin { Mode = new(FlowJoinMode.WaitAny) };
        var afterJoin = new WriteLine("AfterJoin");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, join, afterJoin },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, join),
                CreateConnection(branch2, join),
                CreateConnection(join, afterJoin)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("AfterJoin");

        // At least one branch should execute
        await Assert.That(_output.Lines.Contains("Branch1") || _output.Lines.Contains("Branch2")).IsTrue();

    }

    [Test]
    [DisplayName("Executes join node with WaitAll mode")]
    public async Task ExecutesJoinNodeWaitAll()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var join = new FlowJoin { Mode = new(FlowJoinMode.WaitAll) };
        var afterJoin = new WriteLine("AfterJoin");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, join, afterJoin },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, join),
                CreateConnection(branch2, join),
                CreateConnection(join, afterJoin)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("Branch1");

        await Assert.That(_output.Lines).Contains("Branch2");

        await Assert.That(_output.Lines).Contains("AfterJoin");

    }

    [Test]
    [DisplayName("Handles multiple sequential joins")]
    public async Task HandlesMultipleSequentialJoins()
    {
        // Arrange
        var start = new WriteLine("Start");
        var a1 = new WriteLine("A1");
        var a2 = new WriteLine("A2");
        var join1 = new FlowJoin { Mode = new(FlowJoinMode.WaitAll) };
        var b1 = new WriteLine("B1");
        var b2 = new WriteLine("B2");
        var join2 = new FlowJoin { Mode = new(FlowJoinMode.WaitAll) };
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, a1, a2, join1, b1, b2, join2, end },
            Connections =
            {
                CreateConnection(start, a1),
                CreateConnection(start, a2),
                CreateConnection(a1, join1),
                CreateConnection(a2, join1),
                CreateConnection(join1, b1),
                CreateConnection(join1, b2),
                CreateConnection(b1, join2),
                CreateConnection(b2, join2),
                CreateConnection(join2, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("A1");

        await Assert.That(_output.Lines).Contains("A2");

        await Assert.That(_output.Lines).Contains("B1");

        await Assert.That(_output.Lines).Contains("B2");

        await Assert.That(_output.Lines).Contains("End");

    }

    [Test]
    [DisplayName("Handles complex diamond pattern")]
    public async Task HandlesComplexDiamondPattern()
    {
        // Arrange
        var start = new WriteLine("Start");
        var left1 = new WriteLine("Left1");
        var left2 = new WriteLine("Left2");
        var right1 = new WriteLine("Right1");
        var right2 = new WriteLine("Right2");
        var join = new FlowJoin { Mode = new(FlowJoinMode.WaitAll) };
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, left1, left2, right1, right2, join, end },
            Connections =
            {
                CreateConnection(start, left1),
                CreateConnection(start, right1),
                CreateConnection(left1, left2),
                CreateConnection(right1, right2),
                CreateConnection(left2, join),
                CreateConnection(right2, join),
                CreateConnection(join, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("Left1");

        await Assert.That(_output.Lines).Contains("Left2");

        await Assert.That(_output.Lines).Contains("Right1");

        await Assert.That(_output.Lines).Contains("Right2");

        await Assert.That(_output.Lines).Contains("End");

    }

    [Test]
    [DisplayName("Executes activities in correct order for sequential flow")]
    public async Task ExecutesInCorrectOrderForSequential()
    {
        // Arrange
        var flowchart = CreateSimpleLinearFlowchart(
            new WriteLine("1"),
            new WriteLine("2"),
            new WriteLine("3"),
            new WriteLine("4")
        );

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines.Count).IsEqualTo(4);

        await Assert.That(_output.Lines.ElementAt(0)).IsEqualTo("1");

        await Assert.That(_output.Lines.ElementAt(1)).IsEqualTo("2");

        await Assert.That(_output.Lines.ElementAt(2)).IsEqualTo("3");

        await Assert.That(_output.Lines.ElementAt(3)).IsEqualTo("4");

    }

    [Test]
    [DisplayName("Handles nested flowcharts")]
    public async Task HandlesNestedFlowcharts()
    {
        // Arrange
        var innerFlowchart = CreateSimpleLinearFlowchart(
            new WriteLine("Inner1"),
            new WriteLine("Inner2")
        );

        var outerFlowchart = CreateSimpleLinearFlowchart(
            new WriteLine("Outer1"),
            innerFlowchart,
            new WriteLine("Outer2")
        );

        // Act
        await RunFlowchartAsync(_services, outerFlowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).Contains("Outer1");

        await Assert.That(_output.Lines).Contains("Inner1");

        await Assert.That(_output.Lines).Contains("Inner2");

        await Assert.That(_output.Lines).Contains("Outer2");

    }

    [Test]
    [DisplayName("Handles unconnected activities in flowchart")]
    public async Task HandlesUnconnectedActivities()
    {
        // Arrange
        var connected = new WriteLine("Connected");
        var unconnected = new WriteLine("Unconnected");

        var flowchart = new Flowchart
        {
            Start = connected,
            Activities = { connected, unconnected }
            // No connection to unconnected activity
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert
        await Assert.That(_output.Lines).HasSingleItem();

        await Assert.That(_output.Lines.ElementAt(0)).IsEqualTo("Connected");

        await Assert.That(_output.Lines).DoesNotContain("Unconnected");

    }

    /// <summary>
    /// Regression test for the ordering fix in <see cref="Flowchart.MaybeScheduleWaitAnyActivityAsync"/>:
    /// the outbound activity must be scheduled <em>before</em> remaining inbound branches are canceled.
    /// Previously, canceling first could trigger <c>CompleteIfNoPendingWorkAsync</c> while the outbound
    /// activity was not yet in the scheduler, causing the flowchart to finish prematurely without running
    /// the activity downstream of the join.
    /// </summary>
    [Test]
    [DisplayName("WaitAny join schedules outbound before canceling blocked branch, preventing premature completion")]
    public async Task WaitAnyJoin_SchedulesOutboundBeforeCancelingBlockedBranch()
    {
        // Arrange
        // Flowchart structure:
        //   Start
        //   ├─► Branch1 (fast)  ─► Join (WaitAny) ─► AfterJoin
        //   └─► Branch2 (blocking, creates a bookmark) ─►┘
        //
        // Because the scheduler is LIFO, Branch2 executes first and suspends with a bookmark.
        // Branch1 then completes and triggers the WaitAny join.
        // The join must schedule AfterJoin before canceling Branch2; otherwise
        // CompleteIfNoPendingWorkAsync fires while AfterJoin is not yet queued,
        // completing the flowchart prematurely without executing AfterJoin.
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new BlockingActivity { Id = "BlockingBranch" };
        var join = new FlowJoin { Mode = new(FlowJoinMode.WaitAny) };
        var afterJoin = new WriteLine("AfterJoin");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, join, afterJoin },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, join),
                CreateConnection(branch2, join),
                CreateConnection(join, afterJoin)
            }
        };

        // Act
        var result = await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.CounterBased);

        // Assert: the outbound path of the join must have executed
        await Assert.That(_output.Lines).Contains("AfterJoin");


        // Assert: the workflow must have finished, not suspended waiting for the canceled bookmark
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);


        // Assert: the blocked branch's bookmark was cleared when it was canceled
        await Assert.That(result.WorkflowState.Bookmarks).IsEmpty();

    }

    /// <summary>
    /// A minimal activity that suspends by creating a bookmark, simulating a blocking activity
    /// such as a Delay or Timer that would normally be resumed by an external stimulus.
    /// </summary>
    private sealed class BlockingActivity : Activity
    {
        protected override void Execute(ActivityExecutionContext context) => context.CreateBookmark();
    }
}
