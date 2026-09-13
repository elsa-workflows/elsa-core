using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Branching;

public class FlowDecisionTests
{
    [Test]
    [DisplayName("FlowDecision produces correct outcome and completes successfully")]
    [Arguments(true, "True")]
    [Arguments(false, "False")]
    public async Task Should_Produce_Correct_Outcome_And_Complete(bool condition, string expectedOutcome)
    {
        // Arrange
        var flowDecision = new FlowDecision(ctx => condition);

        // Act
        var context = await ExecuteAsync(flowDecision);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.HasOutcome(expectedOutcome)).IsTrue();
    }

    [Test]
    [DisplayName("FlowDecision defaults to False when no condition is set")]
    public async Task Should_Default_To_False_When_No_Condition_Is_Set()
    {
        // Arrange
        var flowDecision = new FlowDecision();

        // Act
        var context = await ExecuteAsync(flowDecision);

        // Assert
        await Assert.That(context.HasOutcome("False")).IsTrue();
    }

    [Test]
    [DisplayName("FlowDecision evaluates condition exactly once")]
    public async Task Should_Evaluate_Condition_Exactly_Once()
    {
        // Arrange
        var count = 0;
        var flowDecision = new FlowDecision(ctx => { count++; return true; });

        // Act
        await ExecuteAsync(flowDecision);

        // Assert
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    [DisplayName("FlowDecision uses latest captured state when evaluating condition")]
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
        await Assert.That(context.HasOutcome("True")).IsTrue();
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}