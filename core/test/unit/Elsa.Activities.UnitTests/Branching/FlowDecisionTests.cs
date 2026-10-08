using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;

namespace Elsa.Activities.UnitTests.Branching;

public class FlowDecisionTests
{
    [Theory(DisplayName = "FlowDecision produces correct outcome and completes successfully")]
    [InlineData(true, "True")]
    [InlineData(false, "False")]
    public async Task Should_Produce_Correct_Outcome_And_Complete(bool condition, string expectedOutcome)
    {
        // Arrange
        var flowDecision = new FlowDecision(ctx => condition);

        // Act
        var context = await ExecuteAsync(flowDecision);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.True(context.HasOutcome(expectedOutcome));
    }

    [Fact(DisplayName = "FlowDecision defaults to False when no condition is set")]
    public async Task Should_Default_To_False_When_No_Condition_Is_Set()
    {
        // Arrange
        var flowDecision = new FlowDecision();

        // Act
        var context = await ExecuteAsync(flowDecision);

        // Assert
        Assert.True(context.HasOutcome("False"));
    }

    [Fact(DisplayName = "FlowDecision evaluates condition exactly once")]
    public async Task Should_Evaluate_Condition_Exactly_Once()
    {
        // Arrange
        var count = 0;
        var flowDecision = new FlowDecision(ctx => { count++; return true; });

        // Act
        await ExecuteAsync(flowDecision);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact(DisplayName = "FlowDecision uses latest captured state when evaluating condition")]
    public async Task Should_Use_Latest_Captured_State_When_Evaluating_Condition()
    {
        // Arrange
        var flag = false;
        // ReSharper disable once AccessToModifiedClosure
        var flowDecision = new FlowDecision(ctx => flag);

        // Mutate after construction, before execution
        flag = true;

        // Act
        var context = await ExecuteAsync(flowDecision);

        // Assert
        Assert.True(context.HasOutcome("True"));
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
