using Elsa.Testing.Shared;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;
using Elsa.Workflows.Exceptions;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Looping;

public class ForTests
{
    [Test]
    [Arguments(1, 3, 1)] // Ascending loop
    [Arguments(5, 1, -1)] // Descending loop
    [Arguments(-1, -5, -1)] // Descending negative loop
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
        await Assert.That(currentValue).IsEqualTo(start);

        // Verify that child activity is scheduled for valid configurations
        await Assert.That(context.HasScheduledActivity(mockBody)).IsTrue().Because("Expected child activity to be scheduled");
    }

    [Test]
    [Arguments(1, 3, true)] // Inclusive bounds
    [Arguments(1, 3, false)] // Exclusive bounds
    [Arguments(5, 5, true)] // Empty range inclusive
    [Arguments(5, 5, false)] // Empty range exclusive
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
        await Assert.That(context.HasScheduledActivity(mockBody)).IsEqualTo(shouldSchedule);

        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            await Assert.That(currentValue).IsEqualTo(start);
        }
    }

    [Test]
    [Arguments(5, 1, 1)] // Positive step with descending range - won't execute
    [Arguments(1, 5, -1)] // Negative step with ascending range - won't execute
    public async Task SkipsLoopWhenInvalidConfiguration(int start, int end, int step)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.HasScheduledActivity(mockBody)).IsFalse();
    }

    [Test]
    public async Task BodyIsNull_DoesNotScheduleActivity()
    {
        // Arrange
        var forActivity = new For(1, 5, 1) { Body = null };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        var allScheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(allScheduledActivities).IsEmpty();
    }

    [Test]
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
        await Assert.That(currentValue).IsOfType(typeof(int));
        await Assert.That(currentValue).IsEqualTo(1);
    }

    [Test]
    [Arguments(0, 5, true)]  // Start with default value (0), should execute
    [Arguments(1, 0, false)] // End with explicit zero value, should not execute (wrong direction)
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
        await Assert.That(context.HasScheduledActivity(mockBody)).IsEqualTo(shouldExecute);

        if (shouldExecute)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            await Assert.That(currentValue).IsEqualTo(start);
        }
    }

    [Test]
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

    [Test]
    public async Task VerifyBreakBehaviorIsRegistered()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert
        var breakBehavior = forActivity.Behaviors.OfType<BreakBehavior>().FirstOrDefault();
        await Assert.That(breakBehavior).IsNotNull();
    }

    [Test]
    public async Task DefaultPropertyValues()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert
        await Assert.That(forActivity.Start).IsNotNull();
        await Assert.That(forActivity.End).IsNotNull();
        await Assert.That(forActivity.Step).IsNotNull();
        await Assert.That(forActivity.OuterBoundInclusive).IsNotNull();
    }

    [Test]
    [Arguments(1, 3, 1, true)]
    [Arguments(3, 1, -1, true)]
    [Arguments(5, 5, 1, true)]
    [Arguments(1, 5, 0, true)] // Zero step - actually schedules activity (infinite loop potential)
    [Arguments(5, 1, 1, false)] // Wrong direction
    [Arguments(1, 5, -1, false)] // Wrong direction
    public async Task LoopDecisionLogic_ValidatesCorrectly(int start, int end, int step, bool shouldExecute)
    {
        // Arrange
        var mockBody = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.HasScheduledActivity(mockBody)).IsEqualTo(shouldExecute);
    }
    
    // Zero step with different bounds & inclusivity (current contract: schedules at least first body)
    [Test]
    [Arguments(1, 5, true,  true)]  // within ascending range, inclusive -> schedules
    [Arguments(1, 5, false, true)]  // within ascending range, exclusive -> schedules (start < end)
    [Arguments(5, 1, true,  false)] // start > end with ascending step (step=0 treated as positive) -> no schedule
    [Arguments(5, 1, false, false)] // start > end with ascending step (step=0 treated as positive) -> no schedule
    [Arguments(6, 5, true,  false)] // start already outside ascending range -> no schedule
    [Arguments(0, -1, false, false)]// start already outside descending (exclusive) -> no schedule
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
        await Assert.That(context.HasScheduledActivity(body)).IsEqualTo(shouldSchedule);
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            await Assert.That(currentValue).IsOfType(typeof(int));
            await Assert.That(currentValue).IsEqualTo(start);
        }
    }
    
    [Test]
    [Arguments(1, 5, 10)]   // ascending, step too large
    [Arguments(5, 1, -10)]  // descending, step too large
    public async Task StepLargerThanRange_StillSchedulesOnce(int start, int end, int step)
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For(start, end, step) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        await Assert.That(currentValue).IsEqualTo(start);
    }
    
    [Test]
    [Arguments(5, 5,  1)]
    [Arguments(5, 5, -1)]
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
        await Assert.That(context.HasScheduledActivity(body)).IsFalse();
    }
    
    [Test]
    [Arguments(3, 1, -1, true,  true)]  // inclusive: start (3) >= end (1) -> schedules
    [Arguments(3, 3, -1, false, false)] // exclusive: start == end -> no schedule
    [Arguments(2, 3, -1, true,  false)] // start already below end for descending -> no schedule
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
        await Assert.That(context.HasScheduledActivity(body)).IsEqualTo(shouldSchedule);
    }
    
    [Test]
    [Arguments(int.MaxValue, int.MaxValue,  1, true,  true)]  // inclusive equal -> schedules
    [Arguments(int.MaxValue, int.MaxValue,  1, false, false)] // exclusive equal -> no schedule
    [Arguments(int.MinValue, int.MinValue, -1, true,  true)]
    [Arguments(int.MinValue, int.MinValue, -1, false, false)]
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
        await Assert.That(context.HasScheduledActivity(body)).IsEqualTo(shouldSchedule);
        if (shouldSchedule)
        {
            var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
            await Assert.That(currentValue).IsEqualTo(start);
        }
    }
    
    [Test]
    [Arguments("start")]
    [Arguments("end")]
    [Arguments("step")]
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
        var ex = (await Assert.ThrowsExactlyAsync<InputEvaluationException>(() => fixture.ExecuteAsync()))!;
        await Assert.That(ex.Message).Contains(which).WithComparison(StringComparison.InvariantCultureIgnoreCase);
    }
    
    [Test]
    public async Task BodyThrows_ActivitySchedules_WithoutBreaking()
    {
        // Arrange
        var body = new ThrowingBody(new InvalidOperationException("boom"));
        var forActivity = new For(1, 3, 1) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();
        
        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
    }
    
    [Test]
    [Arguments(true,  1, 5)]  // positive step computed -> ascending executes
    [Arguments(false, 5, 1)]  // negative step computed -> descending executes
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
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        await Assert.That(currentValue).IsEqualTo(start);
    }
    
    [Test]
    public async Task NegativeStart_NegativeStep_CurrentValueIsIntAndMatchesStart()
    {
        // Arrange
        var body = new MockBodyActivity();
        var forActivity = new For(-2, -10, -2) { Body = body };
        var fixture = new ActivityTestFixture(forActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
        var currentValue = context.GetActivityOutput(() => forActivity.CurrentValue);
        await Assert.That(currentValue).IsOfType(typeof(int));
        await Assert.That(currentValue).IsEqualTo(-2);
    }
    
    [Test]
    [Arguments(10, 5,  1, true)]  // ascending + inclusive, start > end -> no schedule
    [Arguments(10, 5,  1, false)] // ascending + exclusive, start > end -> no schedule
    [Arguments(0,  5, -1, true)]  // descending + inclusive, start < end -> no schedule
    [Arguments(0,  5, -1, false)] // descending + exclusive, start < end -> no schedule
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
        await Assert.That(context.HasScheduledActivity(body)).IsFalse();
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
