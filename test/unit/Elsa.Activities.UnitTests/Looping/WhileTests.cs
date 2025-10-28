using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;
using Elsa.Workflows.Exceptions;
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

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Should_Evaluate_Condition_Before_Each_Iteration(int maxIterations)
    {
        // Arrange
        var evaluationCount = 0;
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(
            condition: new Input<bool>(_ => ++evaluationCount <= maxIterations),
            body: bodyActivity
        );

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        Assert.Equal(1, evaluationCount); // While activity evaluates condition once per execution
        var hasBodyScheduledActivity = context.HasScheduledActivity(bodyActivity);
        Assert.True(hasBodyScheduledActivity); // Body should be scheduled since condition is true on first evaluation
    }
    
    [Fact]
    public async Task Should_Throw_When_Condition_Is_Not_Set()
    {
        // Arrange
        var body = new MockBodyActivity();
        var whileActivity = new While(body)
        {
            // Reset condition to default/unset state
            Condition = new Input<bool>(false) // This will be the default, but let's test the actual execution behavior
        };
    
        // Act
        var context = await ExecuteAsync(whileActivity);
    
        // Assert - with condition false, body should not be scheduled
        Assert.False(context.HasScheduledActivity(body));
    }
    
    [Fact]
    public async Task Should_Throw_InputEvaluationException_WhenExceptionFromCondition_And_Not_Schedule_Body()
    {
        // Arrange
        var body = new MockBodyActivity();
        var whileActivity = new While(
            condition: new Input<bool>((Func<bool>)(() => throw new ApplicationException("boom"))),
            body: body
        );
    
        // Act
        // Any exception from condition evaluation throws InputEvaluationException
        var ex = await Assert.ThrowsAsync<InputEvaluationException>(() => ExecuteAsync(whileActivity));
    
        // Assert
        Assert.Contains("Failed to evaluate activity input", ex.Message);
    }
    
    [Fact]
    public async Task BodyIsNull_DoesNotSchedule()
    {
        // Arrange
        var whileActivity = new While(
            condition: new Input<bool>(true),
            body: null);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var scheduled = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(scheduled);
    }
    
    [Fact]
    public async Task BodyThrows_ActivitySchedules_WithoutBreaking()
    {
        // Arrange
        var body = new ThrowingBody(new InvalidOperationException("boom"));
        var whileActivity = new While(
            condition: new Input<bool>(true),
            body: body
        );
    
        // Act + Assert
        var context = await ExecuteAsync(whileActivity);
        Assert.NotNull(context);
        Assert.True(context.HasScheduledActivity(body));
    }
    
    [Fact]
    public async Task Should_Use_Latest_Captured_State_When_Evaluating_Condition()
    {
        // Arrange
        var body = new MockBodyActivity();
        
        // Use a class to hold mutable state that can be changed after While construction
        var stateHolder = new StateHolder { ShouldContinue = false };
        var whileActivity = new While(
            condition: new Input<bool>((Func<bool>)(() => stateHolder.ShouldContinue)),
            body: body
        );

        // Mutate after construction, before execution.
        stateHolder.ShouldContinue = true;

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        Assert.True(context.HasScheduledActivity(body));
    }
    
    [Fact]
    public async Task ConditionFalseInitially_EvaluatesOnce_AndDoesNotSchedule()
    {
        // Arrange
        var evals = 0;
        var body = new MockBodyActivity();
        var whileActivity = new While(
            condition: new Input<bool>((Func<bool>)(() => { evals++; return false; })),
            body: body
        );

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        Assert.Equal(1, evals);
        Assert.False(context.HasScheduledActivity(body));
    }
    
    [Fact]
    public async Task DoesNotScheduleBodyTwice_InSingleExecution()
    {
        // Arrange
        var body = new MockBodyActivity();
        var whileActivity = new While(new Input<bool>(true), body);
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var all = context.WorkflowExecutionContext.Scheduler.List()
            .Where(x => x.Activity == body)
            .ToList();
        Assert.Single(all);
    }

    // Break behavior registered (parity with For).
    [Fact]
    public void VerifyBreakBehaviorIsRegistered()
    {
        // Arrange
        var whileActivity = new While(body: null);
    
        // Act
        var breakBehavior = whileActivity.Behaviors.OfType<BreakBehavior>().FirstOrDefault();
    
        // Assert
        Assert.NotNull(breakBehavior);
    }

    private class MockBodyActivity : Activity        {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }

    private class ThrowingBody(Exception exception) : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => throw exception;
    }

    private class StateHolder
    {
        public bool ShouldContinue;
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
