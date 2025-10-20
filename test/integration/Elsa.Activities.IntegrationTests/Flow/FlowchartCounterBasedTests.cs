using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Xunit.Abstractions;
using static Elsa.Activities.IntegrationTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for counter-based flowchart execution strategy.
/// </summary>
public class FlowchartCounterBasedTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output;
    private readonly bool _originalFlowMode;

    public FlowchartCounterBasedTests(ITestOutputHelper testOutputHelper)
    {
        _output = new();
        _services = CreateServiceProvider(testOutputHelper, _output);
        _originalFlowMode = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = false;
    }

    ~FlowchartCounterBasedTests()
    {
        Flowchart.UseTokenFlow = _originalFlowMode;
    }

    [Fact(DisplayName = "Executes simple linear flowchart")]
    public async Task ExecutesSimpleLinearFlowchart()
    {
        // Arrange
        var flowchart = CreateSimpleLinearFlowchart(
            new WriteLine("First"),
            new WriteLine("Second"),
            new WriteLine("Third")
        );

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Equal(3, _output.Lines.Count);
        Assert.Equal("First", _output.Lines.ElementAt(0));
        Assert.Equal("Second", _output.Lines.ElementAt(1));
        Assert.Equal("Third", _output.Lines.ElementAt(2));
    }

    [Fact(DisplayName = "Executes both branches in parallel flowchart")]
    public async Task ExecutesBothBranches()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var flowchart = CreateBranchingFlowchart(start, branch1, branch2);

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Equal(3, _output.Lines.Count);
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Branch1", _output.Lines);
        Assert.Contains("Branch2", _output.Lines);
    }

    [Fact(DisplayName = "Handles flowchart with no connections")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Single(_output.Lines);
        Assert.Equal("Isolated", _output.Lines.ElementAt(0));
    }

    [Fact(DisplayName = "Completes when start activity is null")]
    public async Task CompletesWhenStartIsNull()
    {
        // Arrange
        var flowchart = new Flowchart
        {
            Start = null
        };

        // Act
        var result = await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(_output.Lines);
    }

    [Fact(DisplayName = "Follows conditional branches with If activity")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Single(_output.Lines);
        Assert.Equal("Then branch", _output.Lines.ElementAt(0));
    }

    [Fact(DisplayName = "Executes join node with WaitAny mode")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("AfterJoin", _output.Lines);
        // At least one branch should execute
        Assert.True(_output.Lines.Contains("Branch1") || _output.Lines.Contains("Branch2"));
    }

    [Fact(DisplayName = "Executes join node with WaitAll mode")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Branch1", _output.Lines);
        Assert.Contains("Branch2", _output.Lines);
        Assert.Contains("AfterJoin", _output.Lines);
    }

    [Fact(DisplayName = "Handles multiple sequential joins")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("A1", _output.Lines);
        Assert.Contains("A2", _output.Lines);
        Assert.Contains("B1", _output.Lines);
        Assert.Contains("B2", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Handles complex diamond pattern")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Left1", _output.Lines);
        Assert.Contains("Left2", _output.Lines);
        Assert.Contains("Right1", _output.Lines);
        Assert.Contains("Right2", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Executes activities in correct order for sequential flow")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Equal(4, _output.Lines.Count);
        Assert.Equal("1", _output.Lines.ElementAt(0));
        Assert.Equal("2", _output.Lines.ElementAt(1));
        Assert.Equal("3", _output.Lines.ElementAt(2));
        Assert.Equal("4", _output.Lines.ElementAt(3));
    }

    [Fact(DisplayName = "Handles nested flowcharts")]
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
        await RunFlowchartAsync(_services, outerFlowchart);

        // Assert
        Assert.Contains("Outer1", _output.Lines);
        Assert.Contains("Inner1", _output.Lines);
        Assert.Contains("Inner2", _output.Lines);
        Assert.Contains("Outer2", _output.Lines);
    }

    [Fact(DisplayName = "Handles unconnected activities in flowchart")]
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
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Single(_output.Lines);
        Assert.Equal("Connected", _output.Lines.ElementAt(0));
        Assert.DoesNotContain("Unconnected", _output.Lines);
    }
}
