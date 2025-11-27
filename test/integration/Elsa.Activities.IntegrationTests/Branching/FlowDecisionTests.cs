using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Branching;

/// <summary>
/// Integration tests for FlowDecision activity in flowchart scenarios.
/// </summary>
public class FlowDecisionTests(ITestOutputHelper testOutputHelper) : IDisposable
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    private readonly bool _originalFlowMode = Flowchart.UseTokenFlow;

    public void Dispose()
    {
        Flowchart.UseTokenFlow = _originalFlowMode;
    }

    [Theory(DisplayName = "FlowDecision follows correct path based on condition")]
    [MemberData(nameof(BasicPathTestCases))]
    public async Task Should_Follow_Correct_Path_Based_On_Condition(bool useTokenFlow, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
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
        await _fixture.RunActivityAsync(flowchart);

        // Assert
        AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<object[]> BasicPathTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return [true, true, new[] { "Start", "TruePath" }, new[] { "FalsePath" }];
        yield return [true, false, new[] { "Start", "FalsePath" }, new[] { "TruePath" }];
        yield return [false, true, new[] { "Start", "TruePath" }, new[] { "FalsePath" }];
        yield return [false, false, new[] { "Start", "FalsePath" }, new[] { "TruePath" }];
    }

    [Theory(DisplayName = "FlowDecision handles nested decisions")]
    [MemberData(nameof(NestedDecisionTestCases))]
    public async Task Should_Handle_Nested_Decisions(bool useTokenFlow, bool outerCondition, bool innerCondition, string[] expectedOutputs, string[] unexpectedOutputs)
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
        await _fixture.RunActivityAsync(flowchart);

        // Assert
        AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<object[]> NestedDecisionTestCases()
    {
        // useTokenFlow, outerCondition, innerCondition, expectedOutputs, unexpectedOutputs
        yield return [true, true, true, new[] { "Start", "InnerTrue" }, new[] { "InnerFalse", "OuterFalse" }];
        yield return [true, true, false, new[] { "Start", "InnerFalse" }, new[] { "InnerTrue", "OuterFalse" }];
        yield return [true, false, true, new[] { "Start", "OuterFalse" }, new[] { "InnerTrue", "InnerFalse" }];
        yield return [true, false, false, new[] { "Start", "OuterFalse" }, new[] { "InnerTrue", "InnerFalse" }];
        yield return [false, true, true, new[] { "Start", "InnerTrue" }, new[] { "InnerFalse", "OuterFalse" }];
        yield return [false, true, false, new[] { "Start", "InnerFalse" }, new[] { "InnerTrue", "OuterFalse" }];
        yield return [false, false, true, new[] { "Start", "OuterFalse" }, new[] { "InnerTrue", "InnerFalse" }];
        yield return [false, false, false, new[] { "Start", "OuterFalse" }, new[] { "InnerTrue", "InnerFalse" }];
    }

    [Theory(DisplayName = "FlowDecision works with only one path connected")]
    [MemberData(nameof(OnePathConnectedTestCases))]
    public async Task Should_Work_With_Only_One_Path_Connected(bool useTokenFlow, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
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
        await _fixture.RunActivityAsync(flowchart);

        // Assert
        AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<object[]> OnePathConnectedTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return [true, true, new[] { "Start", "TruePath", "End" }, Array.Empty<string>()];
        yield return [true, false, new[] { "Start" }, new[] { "TruePath", "End" }];
        yield return [false, true, new[] { "Start", "TruePath", "End" }, Array.Empty<string>()];
        yield return [false, false, new[] { "Start" }, new[] { "TruePath", "End" }];
    }

    [Theory(DisplayName = "FlowDecision converges paths correctly")]
    [MemberData(nameof(ConvergePathsTestCases))]
    public async Task Should_Converge_Paths_Correctly(bool useTokenFlow, bool condition, string[] expectedOutputs, string[] unexpectedOutputs)
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
        await _fixture.RunActivityAsync(flowchart);

        // Assert
        AssertOutputs(expectedOutputs, unexpectedOutputs);
    }

    public static IEnumerable<object[]> ConvergePathsTestCases()
    {
        // useTokenFlow, condition, expectedOutputs, unexpectedOutputs
        yield return [true, true, new[] { "Start", "TruePath", "Converge" }, new[] { "FalsePath" }];
        yield return [true, false, new[] { "Start", "FalsePath", "Converge" }, new[] { "TruePath" }];
        yield return [false, true, new[] { "Start", "TruePath", "Converge" }, new[] { "FalsePath" }];
        yield return [false, false, new[] { "Start", "FalsePath", "Converge" }, new[] { "TruePath" }];
    }

    private void AssertOutputs(string[] expectedOutputs, string[] unexpectedOutputs)
    {
        foreach (var expected in expectedOutputs)
        {
            Assert.Contains(expected, _fixture.CapturingTextWriter.Lines);
        }

        foreach (var unexpected in unexpectedOutputs)
        {
            Assert.DoesNotContain(unexpected, _fixture.CapturingTextWriter.Lines);
        }
    }
}
