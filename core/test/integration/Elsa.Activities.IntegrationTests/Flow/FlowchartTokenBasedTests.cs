using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Xunit.Abstractions;
using static Elsa.Activities.IntegrationTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for token-based flowchart execution strategy.
/// </summary>
[Collection("FlowchartTests")]
public class FlowchartTokenBasedTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output;

    public FlowchartTokenBasedTests(ITestOutputHelper testOutputHelper)
    {
        _output = new();
        _services = CreateServiceProvider(testOutputHelper, _output);
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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

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
        var result = await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Single(_output.Lines);
        Assert.Equal("Then branch", _output.Lines.ElementAt(0));
    }

    [Fact(DisplayName = "Executes Stream merge mode - schedules immediately")]
    public async Task ExecutesStreamMergeMode()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var afterJoin = new WriteLine("AfterJoin");
        afterJoin.SetMergeMode(MergeMode.Stream);

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, afterJoin },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, afterJoin),
                CreateConnection(branch2, afterJoin)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("AfterJoin", _output.Lines);
        // In Stream mode, afterJoin executes as soon as first branch arrives
    }

    [Fact(DisplayName = "Executes Race merge mode - cancels other branches")]
    public async Task ExecutesRaceMergeMode()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var afterRace = new WriteLine("AfterRace");
        afterRace.SetMergeMode(MergeMode.Race);

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, afterRace },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, afterRace),
                CreateConnection(branch2, afterRace)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("AfterRace", _output.Lines);
        // In Race mode, afterRace executes on first arrival and blocks others
    }

    [Fact(DisplayName = "Executes Converge merge mode - waits for all branches")]
    public async Task ExecutesConvergeMergeMode()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var converge = new WriteLine("Converge");
        converge.SetMergeMode(MergeMode.Converge);
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, converge, end },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, converge),
                CreateConnection(branch2, converge),
                CreateConnection(converge, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Branch1", _output.Lines);
        Assert.Contains("Branch2", _output.Lines);
        Assert.Contains("Converge", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Executes None merge mode correctly")]
    public async Task ExecutesNoneMergeMode()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var noneMode = new WriteLine("NoneMode");
        noneMode.SetMergeMode(null);

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, noneMode },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, noneMode),
                CreateConnection(branch2, noneMode)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Branch1", _output.Lines);
        Assert.Contains("Branch2", _output.Lines);
        Assert.Contains("NoneMode", _output.Lines);
    }

    [Fact(DisplayName = "Handles token consumption correctly")]
    public async Task HandlesTokenConsumption()
    {
        // Arrange
        var start = new WriteLine("Start");
        var middle = new WriteLine("Middle");
        var end = new WriteLine("End");

        var flowchart = CreateSimpleLinearFlowchart(start, middle, end);

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        // Tokens should be consumed after each activity completes
        Assert.Equal(3, _output.Lines.Count);
        Assert.Equal("Start", _output.Lines.ElementAt(0));
        Assert.Equal("Middle", _output.Lines.ElementAt(1));
        Assert.Equal("End", _output.Lines.ElementAt(2));
    }

    [Fact(DisplayName = "Handles multiple sequential converge nodes")]
    public async Task HandlesMultipleSequentialConvergeNodes()
    {
        // Arrange
        var start = new WriteLine("Start");
        var a1 = new WriteLine("A1");
        var a2 = new WriteLine("A2");
        var converge1 = new WriteLine("Converge1");
        converge1.SetMergeMode(MergeMode.Converge);
        var b1 = new WriteLine("B1");
        var b2 = new WriteLine("B2");
        var converge2 = new WriteLine("Converge2");
        converge2.SetMergeMode(MergeMode.Converge);
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, a1, a2, converge1, b1, b2, converge2, end },
            Connections =
            {
                CreateConnection(start, a1),
                CreateConnection(start, a2),
                CreateConnection(a1, converge1),
                CreateConnection(a2, converge1),
                CreateConnection(converge1, b1),
                CreateConnection(converge1, b2),
                CreateConnection(b1, converge2),
                CreateConnection(b2, converge2),
                CreateConnection(converge2, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("A1", _output.Lines);
        Assert.Contains("A2", _output.Lines);
        Assert.Contains("Converge1", _output.Lines);
        Assert.Contains("B1", _output.Lines);
        Assert.Contains("B2", _output.Lines);
        Assert.Contains("Converge2", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Handles complex diamond pattern with tokens")]
    public async Task HandlesComplexDiamondPattern()
    {
        // Arrange
        var start = new WriteLine("Start");
        var left1 = new WriteLine("Left1");
        var left2 = new WriteLine("Left2");
        var right1 = new WriteLine("Right1");
        var right2 = new WriteLine("Right2");
        var converge = new WriteLine("Converge");
        converge.SetMergeMode(MergeMode.Converge);
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, left1, left2, right1, right2, converge, end },
            Connections =
            {
                CreateConnection(start, left1),
                CreateConnection(start, right1),
                CreateConnection(left1, left2),
                CreateConnection(right1, right2),
                CreateConnection(left2, converge),
                CreateConnection(right2, converge),
                CreateConnection(converge, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Left1", _output.Lines);
        Assert.Contains("Left2", _output.Lines);
        Assert.Contains("Right1", _output.Lines);
        Assert.Contains("Right2", _output.Lines);
        Assert.Contains("Converge", _output.Lines);
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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Equal(4, _output.Lines.Count);
        Assert.Equal("1", _output.Lines.ElementAt(0));
        Assert.Equal("2", _output.Lines.ElementAt(1));
        Assert.Equal("3", _output.Lines.ElementAt(2));
        Assert.Equal("4", _output.Lines.ElementAt(3));
    }

    [Fact(DisplayName = "Handles nested flowcharts with tokens")]
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
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Single(_output.Lines);
        Assert.Equal("Connected", _output.Lines.ElementAt(0));
        Assert.DoesNotContain("Unconnected", _output.Lines);
    }

    [Fact(DisplayName = "Handles mixed merge modes in complex flow")]
    public async Task HandlesMixedMergeModes()
    {
        // Arrange
        var start = new WriteLine("Start");
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var stream = new WriteLine("Stream");
        stream.SetMergeMode(MergeMode.Stream);
        var branch3 = new WriteLine("Branch3");
        var branch4 = new WriteLine("Branch4");
        var converge = new WriteLine("Converge");
        converge.SetMergeMode(MergeMode.Converge);
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2, stream, branch3, branch4, converge, end },
            Connections =
            {
                CreateConnection(start, branch1),
                CreateConnection(start, branch2),
                CreateConnection(branch1, stream),
                CreateConnection(branch2, stream),
                CreateConnection(stream, branch3),
                CreateConnection(stream, branch4),
                CreateConnection(branch3, converge),
                CreateConnection(branch4, converge),
                CreateConnection(converge, end)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Stream", _output.Lines);
        Assert.Contains("Converge", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Handles converge with single inbound connection")]
    public async Task HandlesConvergeWithSingleInbound()
    {
        // Arrange
        var start = new WriteLine("Start");
        var single = new WriteLine("Single");
        single.SetMergeMode(MergeMode.Converge);
        var end = new WriteLine("End");

        var flowchart = CreateSimpleLinearFlowchart(start, single, end);

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Single", _output.Lines);
        Assert.Contains("End", _output.Lines);
    }

    [Fact(DisplayName = "Emits and consumes tokens correctly across multiple steps")]
    public async Task EmitsAndConsumesTokensCorrectly()
    {
        // Arrange
        var step1 = new WriteLine("Step1");
        var step2a = new WriteLine("Step2a");
        var step2b = new WriteLine("Step2b");
        var step3 = new WriteLine("Step3");
        step3.SetMergeMode(MergeMode.Converge);
        var step4 = new WriteLine("Step4");

        var flowchart = new Flowchart
        {
            Start = step1,
            Activities = { step1, step2a, step2b, step3, step4 },
            Connections =
            {
                CreateConnection(step1, step2a),
                CreateConnection(step1, step2b),
                CreateConnection(step2a, step3),
                CreateConnection(step2b, step3),
                CreateConnection(step3, step4)
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, FlowchartExecutionMode.TokenBased);

        // Assert
        // Verify all activities executed in a valid order
        Assert.Contains("Step1", _output.Lines);
        Assert.Contains("Step2a", _output.Lines);
        Assert.Contains("Step2b", _output.Lines);
        Assert.Contains("Step3", _output.Lines);
        Assert.Contains("Step4", _output.Lines);

        // Step3 should only appear once (tokens consumed properly)
        Assert.Single(_output.Lines, l => l == "Step3");
    }
}
