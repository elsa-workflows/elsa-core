using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Xunit.Abstractions;
using static Elsa.Activities.IntegrationTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.IntegrationTests.Branching;

/// <summary>
/// Integration tests for FlowDecision activity in flowchart scenarios.
/// </summary>
public class FlowDecisionTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output;
    private readonly bool _originalFlowMode;

    public FlowDecisionTests(ITestOutputHelper testOutputHelper)
    {
        _output = new();
        _services = CreateServiceProvider(testOutputHelper, _output);
        _originalFlowMode = Flowchart.UseTokenFlow;
    }

    public void Dispose()
    {
        Flowchart.UseTokenFlow = _originalFlowMode;
    }

    [Theory(DisplayName = "FlowDecision follows correct path based on condition")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Should_Follow_Correct_Path_Based_On_Condition(bool useTokenFlow, bool condition)
    {
        // Arrange
        Flowchart.UseTokenFlow = useTokenFlow;

        var start = new WriteLine("Start");
        var decision = new FlowDecision(ctx => condition);
        var truePath = new WriteLine("TruePath");
        var falsePath = new WriteLine("FalsePath");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, decision, truePath, falsePath },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(decision) },
                new() { Source = new(decision, "True"), Target = new(truePath) },
                new() { Source = new(decision, "False"), Target = new(falsePath) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);

        if (condition)
        {
            Assert.Contains("TruePath", _output.Lines);
            Assert.DoesNotContain("FalsePath", _output.Lines);
        }
        else
        {
            Assert.DoesNotContain("TruePath", _output.Lines);
            Assert.Contains("FalsePath", _output.Lines);
        }
    }

    [Theory(DisplayName = "FlowDecision handles nested decisions")]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task Should_Handle_Nested_Decisions(bool useTokenFlow, bool outerCondition, bool innerCondition)
    {
        // Arrange
        Flowchart.UseTokenFlow = useTokenFlow;

        var start = new WriteLine("Start");
        var outerDecision = new FlowDecision(ctx => outerCondition);
        var innerDecision = new FlowDecision(ctx => innerCondition);
        var innerTrue = new WriteLine("InnerTrue");
        var innerFalse = new WriteLine("InnerFalse");
        var outerFalse = new WriteLine("OuterFalse");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, outerDecision, innerDecision, innerTrue, innerFalse, outerFalse },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(outerDecision) },
                new() { Source = new(outerDecision, "True"), Target = new(innerDecision) },
                new() { Source = new(outerDecision, "False"), Target = new(outerFalse) },
                new() { Source = new(innerDecision, "True"), Target = new(innerTrue) },
                new() { Source = new(innerDecision, "False"), Target = new(innerFalse) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);

        if (outerCondition)
        {
            // Outer decision was true, should follow inner decision path
            Assert.DoesNotContain("OuterFalse", _output.Lines);

            if (innerCondition)
            {
                Assert.Contains("InnerTrue", _output.Lines);
                Assert.DoesNotContain("InnerFalse", _output.Lines);
            }
            else
            {
                Assert.DoesNotContain("InnerTrue", _output.Lines);
                Assert.Contains("InnerFalse", _output.Lines);
            }
        }
        else
        {
            // Outer decision was false
            Assert.Contains("OuterFalse", _output.Lines);
            Assert.DoesNotContain("InnerTrue", _output.Lines);
            Assert.DoesNotContain("InnerFalse", _output.Lines);
        }
    }

    [Theory(DisplayName = "FlowDecision works with only one path connected")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Should_Work_With_Only_One_Path_Connected(bool useTokenFlow, bool condition)
    {
        // Arrange
        Flowchart.UseTokenFlow = useTokenFlow;

        var start = new WriteLine("Start");
        var decision = new FlowDecision(ctx => condition);
        var truePath = new WriteLine("TruePath");
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, decision, truePath, end },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(decision) },
                new() { Source = new(decision, "True"), Target = new(truePath) },
                // False path not connected
                new() { Source = new(truePath, "Done"), Target = new(end) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);

        if (condition)
        {
            Assert.Contains("TruePath", _output.Lines);
            Assert.Contains("End", _output.Lines);
        }
        else
        {
            // False path not connected, workflow should end gracefully
            Assert.DoesNotContain("TruePath", _output.Lines);
            Assert.DoesNotContain("End", _output.Lines);
        }
    }

    [Theory(DisplayName = "FlowDecision converges paths correctly")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Should_Converge_Paths_Correctly(bool useTokenFlow, bool condition)
    {
        // Arrange
        Flowchart.UseTokenFlow = useTokenFlow;

        var start = new WriteLine("Start");
        var decision = new FlowDecision(ctx => condition);
        var truePath = new WriteLine("TruePath");
        var falsePath = new WriteLine("FalsePath");
        var converge = new WriteLine("Converge");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, decision, truePath, falsePath, converge },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(decision) },
                new() { Source = new(decision, "True"), Target = new(truePath) },
                new() { Source = new(decision, "False"), Target = new(falsePath) },
                new() { Source = new(truePath, "Done"), Target = new(converge) },
                new() { Source = new(falsePath, "Done"), Target = new(converge) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart);

        // Assert
        Assert.Contains("Start", _output.Lines);
        Assert.Contains("Converge", _output.Lines);

        if (condition)
        {
            Assert.Contains("TruePath", _output.Lines);
            Assert.DoesNotContain("FalsePath", _output.Lines);
        }
        else
        {
            Assert.DoesNotContain("TruePath", _output.Lines);
            Assert.Contains("FalsePath", _output.Lines);
        }
    }
}
