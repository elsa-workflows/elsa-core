using Elsa.Testing.Shared;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Looping;

public class WhileTests
{
    [Fact]
    public async Task Should_Not_Schedule_Body_When_Condition_Is_False()
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(new Input<bool>(false), bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(scheduledActivities);
    }

    [Fact]
    public async Task Should_Schedule_Body_When_Condition_Is_True()
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(new Input<bool>(true), bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Single(scheduledActivities);
        Assert.Equal(bodyActivity, scheduledActivities.First().Activity);
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
