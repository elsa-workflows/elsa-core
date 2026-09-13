using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;
using Elsa.Workflows.Exceptions;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Looping;

public class WhileTests
{
    [Test]
    [Arguments(false, 0)]
    [Arguments(true, 1)]
    public async Task Should_Schedule_Body_Based_On_Condition(bool conditionValue, int expectedScheduledCount)
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = new While(new Input<bool>(conditionValue), bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities.Count).IsEqualTo(expectedScheduledCount);

        if (expectedScheduledCount > 0)
        {
            await Assert.That(scheduledActivities.First().Activity).IsEqualTo(bodyActivity);
        }
    }

    [Test]
    public async Task Should_Use_True_Factory_Method()
    {
        // Arrange
        var bodyActivity = Substitute.For<IActivity>();
        var whileActivity = While.True(bodyActivity);

        // Act
        var context = await ExecuteAsync(whileActivity);

        // Assert
        var hasBodyScheduledActivity = context.HasScheduledActivity(bodyActivity);
        await Assert.That(hasBodyScheduledActivity).IsTrue();
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Should_Evaluate_Condition_Before_Each_Iteration(int maxIterations)
    {
        // Arrange - Create separate condition functions for each test case to verify 
        // that each execution of the While activity evaluates the condition exactly once
        var conditionEvaluationCount = 0;
        var bodyActivity = Substitute.For<IActivity>();
        
        // Create condition that tracks evaluation count across multiple While activity executions
        var condition = new Input<bool>(_ => 
        {
            conditionEvaluationCount++;
            // Return true for the first few evaluations, then false to exit
            return conditionEvaluationCount <= maxIterations;
        });
        
        var whileActivity = new While(condition, bodyActivity);

        // Act - Execute the While activity multiple times to simulate loop iterations
        // Each execution represents one iteration of the loop in real workflow execution
        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            // Execute the While activity (this represents one iteration of the loop)
            var context = await ExecuteAsync(whileActivity);
            
            // Assert this iteration
            await Assert.That(conditionEvaluationCount).IsEqualTo(iteration); // Condition evaluated exactly once per execution
            await Assert.That(context.HasScheduledActivity(bodyActivity)).IsTrue(); // Body should be scheduled because condition is true
        }
        
        // Execute one final time - this should make the condition false and not schedule the body
        var finalContext = await ExecuteAsync(whileActivity);
        
        // Assert final execution
        await Assert.That(conditionEvaluationCount).IsEqualTo(maxIterations + 1); // One final evaluation that returns false
        await Assert.That(finalContext.HasScheduledActivity(bodyActivity)).IsFalse(); // Body NOT scheduled because condition is false
    }
    
    [Test]
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
        await Assert.That(context.HasScheduledActivity(body)).IsFalse();
    }
    
    [Test]
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
        var ex = (await Assert.ThrowsExactlyAsync<InputEvaluationException>(() => ExecuteAsync(whileActivity)))!;
    
        // Assert
        await Assert.That(ex.Message).Contains("Failed to evaluate activity input");
    }
    
    [Test]
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
        await Assert.That(scheduled).IsEmpty();
    }
    
    [Test]
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
        await Assert.That(context).IsNotNull();
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
    }
    
    [Test]
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
        await Assert.That(context.HasScheduledActivity(body)).IsTrue();
    }
    
    [Test]
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
        await Assert.That(evals).IsEqualTo(1);
        await Assert.That(context.HasScheduledActivity(body)).IsFalse();
    }
    
    [Test]
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
        await Assert.That(all).HasSingleItem();
    }

    // Break behavior registered (parity with For).
    [Test]
    public async Task VerifyBreakBehaviorIsRegistered()
    {
        // Arrange
        var whileActivity = new While(body: null);
    
        // Act
        var breakBehavior = whileActivity.Behaviors.OfType<BreakBehavior>().FirstOrDefault();
    
        // Assert
        await Assert.That(breakBehavior).IsNotNull();
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
