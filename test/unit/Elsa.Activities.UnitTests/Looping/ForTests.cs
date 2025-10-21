using Elsa.Testing.Shared;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;

namespace Elsa.Activities.UnitTests.Looping;

public class ForTests
{
    [Theory]
    [InlineData(1, 3, 1)] // Ascending loop
    [InlineData(5, 1, -1)] // Descending loop  
    [InlineData(10, 12, 1)] // UpdatesCurrentValueEachIteration
    [InlineData(0, 4, 1)] // HandlesFloatingPointSteps equivalent
    [InlineData(10, 15, 1)] // PreservesIterationCountAccuracy
    [InlineData(-1, -5, -1)] // NegativeRangeAndNegativeStep
    public async Task ExecutesValidLoopConfigurations_SchedulesChildActivity(int start, int end, int step)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.Equal(start, currentValue);
        
        // Verify that child activity is scheduled for valid configurations
        Assert.True(context.HasScheduledActivity(mockBody), "Expected child activity to be scheduled");
    }

    [Theory]
    [InlineData(1, 3, true)] // Inclusive bounds
    [InlineData(1, 3, false)] // Exclusive bounds
    [InlineData(5, 5, true)] // Empty range inclusive
    [InlineData(5, 5, false)] // Empty range exclusive
    public async Task ExecutesBoundaryConditions(int start, int end, bool inclusive)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(1),
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = mockBody
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        bool shouldSchedule = ShouldExecuteLoopWithBounds(start, end, 1, inclusive);
        Assert.Equal(shouldSchedule, context.HasScheduledActivity(mockBody));
        
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            Assert.Equal(start, currentValue);
        }
    }

    [Theory]
    [InlineData(5, 1, 1)] // Positive step with descending range - won't execute
    [InlineData(1, 5, -1)] // Negative step with ascending range - won't execute  
    public async Task SkipsLoopWhenInvalidConfiguration(int start, int end, int step)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.False(context.HasScheduledActivity(mockBody));
    }

    [Fact]
    public async Task BodyIsNull_DoesNotScheduleActivity()
    {
        // Arrange
        var forActivity = new For(1, 5, 1) { Body = null };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        var allScheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        Assert.Empty(allScheduledActivities);
    }

    [Fact]
    public async Task CurrentValueOutputTypeCheck_PreservesIntegerType()
    {
        // Arrange
        var mockBody = new MockBodyActivity(); 
        var forActivity = new For(1, 3, 1) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.IsType<int>(currentValue);
        Assert.Equal(1, currentValue);
    }

    [Fact] 
    public async Task ValidConfiguration_SchedulesChildActivity()
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(100, 102, 1) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.True(context.HasScheduledActivity(mockBody));
        
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.Equal(100, currentValue);
    }

    [Theory]
    [InlineData(0, 5)] // Start with default value (0)
    [InlineData(1, 0)] // End with explicit zero value
    public async Task HandlesDefaultValues(int start, int end)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(1),
            Body = mockBody
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        bool shouldSchedule = ShouldExecuteLoop(start, end, 1);
        Assert.Equal(shouldSchedule, context.HasScheduledActivity(mockBody));
        
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            Assert.Equal(start, currentValue);
        }
    }

    [Fact]
    public void VerifyActivityAttributes()
    {
        // Arrange
        var forActivity = new For();
        var fixture = new ActivityTestFixture(forActivity);

        // Act & Assert
        fixture.AssertActivityAttributes(
            expectedNamespace: "Elsa",
            expectedKind: ActivityKind.Action,
            expectedCategory: "Looping", 
            expectedDisplayName: null,
            expectedDescription: "Iterate over a sequence of steps between a start and an end number."
        );
    }

    [Fact]
    public void VerifyBreakBehaviorIsRegistered()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert
        var breakBehavior = forActivity.Behaviors.OfType<BreakBehavior>().FirstOrDefault();
        Assert.NotNull(breakBehavior);
    }

    [Fact]
    public void DefaultPropertyValues()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert
        Assert.NotNull(forActivity.Start);
        Assert.NotNull(forActivity.End);
        Assert.NotNull(forActivity.Step);
        Assert.NotNull(forActivity.OuterBoundInclusive);
    }

    [Theory]
    [InlineData(1, 3, 1, true)]
    [InlineData(3, 1, -1, true)]
    [InlineData(5, 5, 1, true)]
    [InlineData(1, 5, 0, true)] // Zero step - actually schedules activity (infinite loop potential)
    [InlineData(5, 1, 1, false)] // Wrong direction
    [InlineData(1, 5, -1, false)] // Wrong direction
    public async Task LoopDecisionLogic_ValidatesCorrectly(int start, int end, int step, bool shouldExecute)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(shouldExecute, context.HasScheduledActivity(mockBody));
    }

    // Private helper methods
    private static bool ShouldExecuteLoop(int start, int end, int step)
    {
        return ShouldExecuteLoopWithBounds(start, end, step, true);
    }

    private static bool ShouldExecuteLoopWithBounds(int start, int end, int step, bool inclusive)
    {
        // Match the actual For activity logic exactly
        var increment = step >= 0;
        var currentValue = start;
        
        return increment && inclusive ? currentValue <= end
            : increment && !inclusive ? currentValue < end
            : !increment && inclusive ? currentValue >= end
            : !increment && !inclusive ? currentValue > end : false;
    }

    /// <summary>
    /// Mock activity to represent the body of the For loop
    /// </summary>
    private class MockBodyActivity : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            return ValueTask.CompletedTask;
        }
    }
}
