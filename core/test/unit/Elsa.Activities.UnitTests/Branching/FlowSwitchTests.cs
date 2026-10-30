using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Activities.UnitTests.Branching;

public class FlowSwitchTests
{
    [Theory]
    [MemberData(nameof(DefaultOutcomeTestCases))]
    public async Task Should_Return_Default_When_No_Cases_Match(List<FlowSwitchCase> cases)
    {
        // Arrange
        var flowSwitch = new FlowSwitch { Cases = cases };

        // Act
        var context = await ExecuteAsync(flowSwitch);

        // Assert - Activity should return Default outcome when no cases match.
        Assert.True(context.HasOutcome("Default"));
    }

    [Theory]
    [MemberData(nameof(SwitchModeTestCases))]
    public async Task Should_Return_Outcomes_Based_On_Switch_Mode(
        List<FlowSwitchCase> cases,
        SwitchMode mode,
        string[] expectedOutcomes)
    {
        // Arrange
        var flowSwitch = new FlowSwitch
        {
            Cases = cases,
            Mode = new(mode)
        };

        // Act
        var context = await ExecuteAsync(flowSwitch);

        // Assert - Activity should return outcomes based on switch mode.
        var outcomes = context.GetOutcomes().ToList();
        Assert.Equal(expectedOutcomes.Length, outcomes.Count);
        foreach (var expectedOutcome in expectedOutcomes)
            Assert.Contains(expectedOutcome, outcomes);
    }

    [Fact]
    public async Task Should_Evaluate_Cases_With_Literal_Expression()
    {
        // Arrange
        var flowSwitch = new FlowSwitch
        {
            Cases = new List<FlowSwitchCase>
            {
                new("TrueCase", Expression.LiteralExpression(true)),
                new("FalseCase", Expression.LiteralExpression(false))
            }
        };

        // Act
        var context = await ExecuteAsync(flowSwitch);

        // Assert - Activity should match the case with true literal.
        Assert.True(context.HasOutcome("TrueCase"));
    }
    
    public static IEnumerable<object[]> DefaultOutcomeTestCases()
    {
        yield return [new List<FlowSwitchCase>()];
        yield return
        [
            new List<FlowSwitchCase>
            {
                new("Case1", () => false),
                new("Case2", () => false)
            }
        ];
    }
    
    public static IEnumerable<object[]> SwitchModeTestCases()
    {
        var cases = new List<FlowSwitchCase>
        {
            new("Case1", () => false),
            new("Case2", () => true),
            new("Case3", () => true)
        };

        yield return [cases, SwitchMode.MatchFirst, new[] { "Case2" }];
        yield return [cases, SwitchMode.MatchAny, new[] { "Case2", "Case3" }];
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
