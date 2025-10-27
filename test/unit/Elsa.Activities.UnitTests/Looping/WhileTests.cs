using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Looping;

public class WhileTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task Should_Schedule_Body_Based_On_Condition(bool conditionValue, int expectedScheduledCount)
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(new Input<bool>(conditionValue), bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Equal(expectedScheduledCount, scheduledActivities.Count);

        if (expectedScheduledCount > 0)
        {
            Assert.Equal(bodyActivity, scheduledActivities.First().Activity);
        }
    }

    [Fact]
    public async Task Should_Use_True_Factory_Method()
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = While.True(bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var hasBodyScheduledActivity = context.HasScheduledActivity(bodyActivity);
        Assert.True(hasBodyScheduledActivity);
    }

    [Fact]
    public async Task Should_Evaluate_Condition_Before_Each_Iteration()
    {
        // Arrange
        var evaluationCount = 0;
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(
            condition: new Input<bool>(_ => ++evaluationCount <= 1), // Only true for first evaluation
            body: bodyActivity
        );

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        Assert.Equal(1, evaluationCount); // Condition should be evaluated once
        var hasBodyScheduledActivity = context.HasScheduledActivity(bodyActivity);
        Assert.True(hasBodyScheduledActivity);
    }

    // Helper methods
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
