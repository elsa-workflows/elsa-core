using Elsa.Testing.Shared;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;
using Elsa.Workflows.Exceptions;

namespace Elsa.Activities.UnitTests.Looping;

public class ForTests
{
    [Theory]
    [InlineData(1, 3, 1)] // Ascending loop
    [InlineData(5, 1, -1)] // Descending loop  
    [InlineData(-1, -5, -1)] // Descending negative loop
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

    [Theory]
    [InlineData(0, 5, true)]  // Start with default value (0), should execute
    [InlineData(1, 0, false)] // End with explicit zero value, should not execute (wrong direction)
    public async Task HandlesDefaultValues(int start, int end, bool shouldExecute)
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
        Assert.Equal(shouldExecute, context.HasScheduledActivity(mockBody));
        
        if (shouldExecute)
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
    
    // Zero step with different bounds & inclusivity (current contract: schedules at least first body)
    [Theory]
    [InlineData(1, 5, true,  true)]  // within ascending range, inclusive -> schedules
    [InlineData(1, 5, false, true)]  // within ascending range, exclusive -> schedules (start < end)
    [InlineData(5, 1, true,  false)] // start > end with ascending step (step=0 treated as positive) -> no schedule
    [InlineData(5, 1, false, false)] // start > end with ascending step (step=0 treated as positive) -> no schedule
    [InlineData(6, 5, true,  false)] // start already outside ascending range -> no schedule
    [InlineData(0, -1, false, false)]// start already outside descending (exclusive) -> no schedule
    public async Task ZeroStep_RespectsInitialBoundCheck(int start, int end, bool inclusive, bool shouldSchedule)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(0), // zero step: current contract allows first schedule
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(shouldSchedule, context.HasScheduledActivity(body));
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            Assert.IsType<int>(currentValue);
            Assert.Equal(start, currentValue);
        }
    }
    
    [Theory]
    [InlineData(1, 5, 10)]   // ascending, step too large
    [InlineData(5, 1, -10)]  // descending, step too large
    public async Task StepLargerThanRange_StillSchedulesOnce(int start, int end, int step)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.True(context.HasScheduledActivity(body));
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.Equal(start, currentValue);
    }
    
    [Theory]
    [InlineData(5, 5,  1)]
    [InlineData(5, 5, -1)]
    public async Task EqualBounds_Exclusive_DoesNotExecute(int start, int end, int step)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(step),
            OuterBoundInclusive = new Input<bool>(false),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.False(context.HasScheduledActivity(body));
    }
    
    [Theory]
    [InlineData(3, 1, -1, true,  true)]  // inclusive: start (3) >= end (1) -> schedules
    [InlineData(3, 3, -1, false, false)] // exclusive: start == end -> no schedule
    [InlineData(2, 3, -1, true,  false)] // start already below end for descending -> no schedule
    public async Task Descending_InclusiveExclusive_OffByOne(int start, int end, int step, bool inclusive, bool shouldSchedule)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(step),
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(shouldSchedule, context.HasScheduledActivity(body));
    }
    
    [Theory]
    [InlineData(int.MaxValue, int.MaxValue,  1, true,  true)]  // inclusive equal -> schedules
    [InlineData(int.MaxValue, int.MaxValue,  1, false, false)] // exclusive equal -> no schedule
    [InlineData(int.MinValue, int.MinValue, -1, true,  true)]
    [InlineData(int.MinValue, int.MinValue, -1, false, false)]
    public async Task ExtremeBounds_NoOverflow_OnInitialDecision(int start, int end, int step, bool inclusive, bool shouldSchedule)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(step),
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(shouldSchedule, context.HasScheduledActivity(body));
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            Assert.Equal(start, currentValue);
        }
    }
    
    [Theory]
    [InlineData("start")]
    [InlineData("end")]
    [InlineData("step")]
    public async Task InputExpressions_Throw_DoNotSchedule(string which)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = which == "start" ? new Input<int>((Func<int>)(() => throw new ApplicationException("start!"))) : new Input<int>(1),
            End   = which == "end"   ? new Input<int>((Func<int>)(() => throw new ApplicationException("end!")))   : new Input<int>(3),
            Step  = which == "step"  ? new Input<int>((Func<int>)(() => throw new ApplicationException("step!")))  : new Input<int>(1),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InputEvaluationException>(() => fixture.ExecuteAsync());
        Assert.Contains(which, ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
    
    [Fact]
    public async Task BodyThrows_ActivitySchedules_WithoutBreaking()
    {
        // Arrange
        var body = new ThrowingBody(new InvalidOperationException("boom"));
        var forActivity = new For(1, 3, 1) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();
        
        // Assert
        Assert.NotNull(context);
        Assert.True(context.HasScheduledActivity(body));
    }
    
    [Theory]
    [InlineData(true,  1, 5)]  // positive step computed -> ascending executes
    [InlineData(false, 5, 1)]  // negative step computed -> descending executes
    public async Task DynamicStep_EvaluatedAtExecutionTime(bool usePositive, int start, int end)
    {
        // Arrange
        var body = new MockBodyActivity();
        var stepValue = usePositive ? 1 : -1; // Compute step value directly from parameter
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End   = new Input<int>(end),
            Step  = new Input<int>((Func<int>)(() => stepValue)),
            Body  = body
        };

        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.True(context.HasScheduledActivity(body));
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.Equal(start, currentValue);
    }
    
    [Fact]
    public async Task NegativeStart_NegativeStep_CurrentValueIsIntAndMatchesStart()
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For(-2, -10, -2) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.True(context.HasScheduledActivity(body));
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        Assert.IsType<int>(currentValue);
        Assert.Equal(-2, currentValue);
    }
    
    [Theory]
    [InlineData(10, 5,  1, true)]  // ascending + inclusive, start > end -> no schedule
    [InlineData(10, 5,  1, false)] // ascending + exclusive, start > end -> no schedule
    [InlineData(0,  5, -1, true)]  // descending + inclusive, start < end -> no schedule
    [InlineData(0,  5, -1, false)] // descending + exclusive, start < end -> no schedule
    public async Task StartOutsideRange_DoesNotSchedule(int start, int end, int step, bool inclusive)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End   = new Input<int>(end),
            Step  = new Input<int>(step),
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = body
        };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.False(context.HasScheduledActivity(body));
    }
    
    private static bool ShouldExecuteLoopWithBounds(int start, int end, int step, bool inclusive)
    {
        // Match the actual For activity logic exactly
        var increment = step >= 0;

        return increment && inclusive ? start <= end
            : increment && !inclusive ? start < end
            : !increment && inclusive ? start >= end
            : !increment && !inclusive && start > end;
    }

    /// <summary>
    /// Mock activity to represent the body of the For loop
    /// </summary>
    private class MockBodyActivity : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }
    
    /// <summary>
    /// A body that throws a provided exception when executed
    /// </summary>
    private class ThrowingBody(Exception exception) : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => throw exception;
    }
}
