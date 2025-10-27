using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;

namespace Elsa.Activities.UnitTests.Branching;

public class IfTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Set_Result_To_Condition_Value_Regardless_Of_Branch_Presence(bool conditionValue)
    {
        // Arrange - Test with no branches to verify result is independent of branch activities
        var ifActivity = new If(() => conditionValue);

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.Equal(conditionValue, resultValue);
    }

    [Fact]
    public async Task Should_Schedule_Then_Branch_When_Condition_Is_True_And_Then_Branch_Exists()
    {
        // Arrange
        var ifActivity = new If(() => true);
        var thenActivity = new WriteLine("then executed");
        ifActivity.Then = thenActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.True(resultValue);
        Assert.True(context.HasScheduledActivity(thenActivity), "Then branch should be scheduled when condition is true");
    }

    [Fact]
    public async Task Should_Schedule_Else_Branch_When_Condition_Is_False_And_Else_Branch_Exists()
    {
        // Arrange
        var ifActivity = new If(() => false);
        var elseActivity = new WriteLine("else executed");
        ifActivity.Else = elseActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.False(resultValue);
        Assert.True(context.HasScheduledActivity(elseActivity), "Else branch should be scheduled when condition is false");
    }

    [Fact]
    public async Task Should_Schedule_Only_Then_Branch_When_Condition_Is_True_And_Both_Branches_Exist()
    {
        // Arrange
        var ifActivity = new If(() => true);
        var thenActivity = new WriteLine("then executed");
        var elseActivity = new WriteLine("else executed");
        ifActivity.Then = thenActivity;
        ifActivity.Else = elseActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.True(resultValue);
        Assert.True(context.HasScheduledActivity(thenActivity), "Then branch should be scheduled when condition is true");
        Assert.False(context.HasScheduledActivity(elseActivity), "Else branch should not be scheduled when condition is true");
    }

    [Fact]
    public async Task Should_Schedule_Only_Else_Branch_When_Condition_Is_False_And_Both_Branches_Exist()
    {
        // Arrange
        var ifActivity = new If(() => false);
        var thenActivity = new WriteLine("then executed");
        var elseActivity = new WriteLine("else executed");
        ifActivity.Then = thenActivity;
        ifActivity.Else = elseActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.False(resultValue);
        Assert.True(context.HasScheduledActivity(elseActivity), "Else branch should be scheduled when condition is false");
        Assert.False(context.HasScheduledActivity(thenActivity), "Then branch should not be scheduled when condition is false");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Not_Throw_When_Only_Then_Branch_Is_Present(bool conditionValue)
    {
        // Arrange
        var ifActivity = new If(() => conditionValue)
        {
            Then = new WriteLine("then branch")
        };

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.Equal(conditionValue, resultValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Not_Throw_When_Only_Else_Branch_Is_Present(bool conditionValue)
    {
        // Arrange
        var ifActivity = new If(() => conditionValue)
        {
            Else = new WriteLine("else branch")
        };

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.Equal(conditionValue, resultValue);
    }

    [Fact]
    public async Task Should_Not_Schedule_Then_Branch_When_Condition_Is_False_And_Only_Then_Branch_Exists()
    {
        // Arrange
        var ifActivity = new If(() => false);
        var thenActivity = new WriteLine("then executed");
        ifActivity.Then = thenActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.False(resultValue);
        Assert.False(context.HasScheduledActivity(thenActivity), "Then branch should not be scheduled when condition is false");
    }

    [Fact]
    public async Task Should_Not_Schedule_Else_Branch_When_Condition_Is_True_And_Only_Else_Branch_Exists()
    {
        // Arrange
        var ifActivity = new If(() => true);
        var elseActivity = new WriteLine("else executed");
        ifActivity.Else = elseActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.True(resultValue);
        Assert.False(context.HasScheduledActivity(elseActivity), "Else branch should not be scheduled when condition is true");
    }
    
    [Fact]
    public async Task Should_Not_Throw_Error_When_Condition_Is_Not_Set()
    {
        // Arrange
        var ifActivity = new If(); // no condition provided
        var thenActivity = new WriteLine("then");
        ifActivity.Then = thenActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);
        
        // Assert
        Assert.NotNull(context);
    }
    
    [Fact]
    public async Task Should_Bubble_Exception_From_Condition_And_Not_Schedule_Any_Branch()
    {
        // Arrange
        var ifActivity = new If(() =>  throw new ApplicationException("boom"));
        var thenActivity = new WriteLine("then");
        var elseActivity = new WriteLine("else");
        ifActivity.Then = thenActivity;
        ifActivity.Else = elseActivity;

        // Act - Throwing any kind of exception in the condition results in an InputEvaluationException
        var ex = await Assert.ThrowsAsync<InputEvaluationException>(() => ExecuteAsync(ifActivity));

        // Assert
        Assert.Contains("Failed to evaluate", ex.Message);
    }
    
    [Fact]
    public async Task Should_Evaluate_Condition_Exactly_Once()
    {
        // Arrange
        var count = 0;
        var ifActivity = new If(() => { count++; return true; });
        var thenActivity = new WriteLine("then");
        ifActivity.Then = thenActivity;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        Assert.Equal(1, count);
        Assert.True(context.HasScheduledActivity(thenActivity));
    }
    
    [Fact]
    public async Task Should_Use_Latest_Captured_State_When_Evaluating_Condition()
    {
        // Arrange
        var flag = false;
        // ReSharper disable once AccessToModifiedClosure
        var ifActivity = new If(() => flag);
        var thenActivity = new WriteLine("then");
        var elseActivity = new WriteLine("else");
        ifActivity.Then = thenActivity;
        ifActivity.Else = elseActivity;

        // Mutate after construction, before execution
        flag = true;

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.True(resultValue);
        Assert.True(context.HasScheduledActivity(thenActivity));
        Assert.False(context.HasScheduledActivity(elseActivity));
    }
    
    [Theory]           // outer, inner
    [InlineData(true,  true)]
    [InlineData(true,  false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Should_Schedule_Correct_Branches_For_Nested_If(bool outerCondition, bool innerCondition)
    {
        // Arrange inner
        var innerThen = new WriteLine("inner-then");
        var innerElse = new WriteLine("inner-else");
        var innerIf = new If(() => innerCondition)
        {
            Then = innerThen,
            Else = innerElse
        };

        // Arrange outer
        var outerElse = new WriteLine("outer-else");
        var outerIf = new If(() => outerCondition)
        {
            Then = innerIf,
            Else = outerElse
        };

        // Act
        var context = await ExecuteAsync(outerIf);

        // Assert outer result & scheduling
        var outerResult = (bool)context.GetActivityOutput(() => outerIf.Result)!;
        Assert.Equal(outerCondition, outerResult);

        if (outerCondition)
        {
            // When outer condition is true, the inner If should be scheduled
            Assert.True(context.HasScheduledActivity(innerIf), 
                "Inner If should be scheduled when outer condition is true");
            
            // The outer else should NOT be scheduled
            Assert.False(context.HasScheduledActivity(outerElse), 
                "Outer else should not be scheduled when outer condition is true");
            
            // The inner branches should NOT be scheduled yet because the inner If hasn't executed
            Assert.False(context.HasScheduledActivity(innerThen), 
                "Inner then should not be scheduled yet - inner If hasn't executed");
            Assert.False(context.HasScheduledActivity(innerElse), 
                "Inner else should not be scheduled yet - inner If hasn't executed");
            
            // Note: The inner If result won't be available until it executes, so we can't assert it
        }
        else
        {
            // When outer condition is false, outer else should be scheduled
            Assert.True(context.HasScheduledActivity(outerElse), 
                "Outer else should be scheduled when outer condition is false");
            
            // The inner If and its branches should NOT be scheduled
            Assert.False(context.HasScheduledActivity(innerIf), 
                "Inner If should not be scheduled when outer condition is false");
            Assert.False(context.HasScheduledActivity(innerThen), 
                "Inner then should not be scheduled when outer condition is false");
            Assert.False(context.HasScheduledActivity(innerElse), 
                "Inner else should not be scheduled when outer condition is false");
        }
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
