using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Options;

namespace Elsa.Activities.IntegrationTests.Branching;

/// <summary>
/// Integration tests for FlowDecision activity in flowchart scenarios.
/// </summary>
public class FlowDecisionTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("FlowDecision follows correct path based on condition ($executionMode, condition: $condition)")]
    [MethodDataSource(nameof(BasicPathTestCases))]
    public async Task Should_Follow_Correct_Path_Based_On_Condition(FlowchartExecutionMode executionMode, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
    {
        // Arrange
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

        var options = new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode);

        // Act
        await _fixture.RunActivityAsync(flowchart, options);

        // Assert
        await AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<Func<(FlowchartExecutionMode, bool, string[], string[])>> BasicPathTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return () => (FlowchartExecutionMode.TokenBased, true, ["Start", "TruePath"], ["FalsePath"]);
        yield return () => (FlowchartExecutionMode.TokenBased, false, ["Start", "FalsePath"], ["TruePath"]);
        yield return () => (FlowchartExecutionMode.CounterBased, true, ["Start", "TruePath"], ["FalsePath"]);
        yield return () => (FlowchartExecutionMode.CounterBased, false, ["Start", "FalsePath"], ["TruePath"]);
    }

    [Test]
    [DisplayName("FlowDecision handles nested decisions ($executionMode, outer: $outerCondition, inner: $innerCondition)")]
    [MethodDataSource(nameof(NestedDecisionTestCases))]
    public async Task Should_Handle_Nested_Decisions(FlowchartExecutionMode executionMode, bool outerCondition, bool innerCondition, string[] expectedOutputs, string[] unexpectedOutputs)
    {
        // Arrange
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

        var options = new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode);

        // Act
        await _fixture.RunActivityAsync(flowchart, options);

        // Assert
        await AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<Func<(FlowchartExecutionMode, bool, bool, string[], string[])>> NestedDecisionTestCases()
    {
        // useTokenFlow, outerCondition, innerCondition, expectedOutputs, unexpectedOutputs
        yield return () => (FlowchartExecutionMode.TokenBased, true, true, ["Start", "InnerTrue"], ["InnerFalse", "OuterFalse"]);
        yield return () => (FlowchartExecutionMode.TokenBased, true, false, ["Start", "InnerFalse"], ["InnerTrue", "OuterFalse"]);
        yield return () => (FlowchartExecutionMode.TokenBased, false, true, ["Start", "OuterFalse"], ["InnerTrue", "InnerFalse"]);
        yield return () => (FlowchartExecutionMode.TokenBased, false, false, ["Start", "OuterFalse"], ["InnerTrue", "InnerFalse"]);
        yield return () => (FlowchartExecutionMode.CounterBased, true, true, ["Start", "InnerTrue"], ["InnerFalse", "OuterFalse"]);
        yield return () => (FlowchartExecutionMode.CounterBased, true, false, ["Start", "InnerFalse"], ["InnerTrue", "OuterFalse"]);
        yield return () => (FlowchartExecutionMode.CounterBased, false, true, ["Start", "OuterFalse"], ["InnerTrue", "InnerFalse"]);
        yield return () => (FlowchartExecutionMode.CounterBased, false, false, ["Start", "OuterFalse"], ["InnerTrue", "InnerFalse"]);
    }

    [Test]
    [DisplayName("FlowDecision works with only one path connected ($executionMode, condition: $condition)")]
    [MethodDataSource(nameof(OnePathConnectedTestCases))]
    public async Task Should_Work_With_Only_One_Path_Connected(FlowchartExecutionMode executionMode, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
    {
        // Arrange
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

        var options = new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode);

        // Act
        await _fixture.RunActivityAsync(flowchart, options);

        // Assert
        await AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<Func<(FlowchartExecutionMode, bool, string[], string[])>> OnePathConnectedTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return () => (FlowchartExecutionMode.TokenBased, true, ["Start", "TruePath", "End"], []);
        yield return () => (FlowchartExecutionMode.TokenBased, false, ["Start"], ["TruePath", "End"]);
        yield return () => (FlowchartExecutionMode.CounterBased, true, ["Start", "TruePath", "End"], []);
        yield return () => (FlowchartExecutionMode.CounterBased, false, ["Start"], ["TruePath", "End"]);
    }

    [Test]
    [DisplayName("FlowDecision converges paths correctly ($executionMode, condition: $condition)")]
    [MethodDataSource(nameof(ConvergePathsTestCases))]
    public async Task Should_Converge_Paths_Correctly(FlowchartExecutionMode executionMode, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
    {
        // Arrange
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

        var options = new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode);

        // Act
        await _fixture.RunActivityAsync(flowchart, options);

        // Assert
        await AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<Func<(FlowchartExecutionMode, bool, string[], string[])>> ConvergePathsTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return () => (FlowchartExecutionMode.TokenBased, true, ["Start", "TruePath", "Converge"], ["FalsePath"]);
        yield return () => (FlowchartExecutionMode.TokenBased, false, ["Start", "FalsePath", "Converge"], ["TruePath"]);
        yield return () => (FlowchartExecutionMode.CounterBased, true, ["Start", "TruePath", "Converge"], ["FalsePath"]);
        yield return () => (FlowchartExecutionMode.CounterBased, false, ["Start", "FalsePath", "Converge"], ["TruePath"]);
    }

    private async Task AssertOutputs(string[] expectedOutputs, string[] unexpectedOutputs)
    {
        foreach (var expected in expectedOutputs)
            await Assert.That(_fixture.CapturingTextWriter.Lines).Contains(expected);
        foreach (var unexpected in unexpectedOutputs)
            await Assert.That(_fixture.CapturingTextWriter.Lines).DoesNotContain(unexpected);
    }
}
