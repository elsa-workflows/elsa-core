using Elsa.Activities.UnitTests.Helpers;
using Elsa.Extensions;

namespace Elsa.Activities.UnitTests.Branching;

public class IfTests
{
    [Fact]
    public async Task Should_Evaluate_Condition_And_Set_Result_When_Activity_Runs()
    {
        // Arrange
        var ifActivity = new If(() => true);

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_To_True_When_Condition_Is_True()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var thenActivity = new SetVariable<string>(variable, new Input<string>("then_executed"));
        
        var ifActivity = new If(() => true)
        {
            Then = thenActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_To_False_When_Condition_Is_False()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var elseActivity = new SetVariable<string>(variable, new Input<string>("else_executed"));
        
        var ifActivity = new If(() => false)
        {
            Else = elseActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.False(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_To_True_When_Condition_Is_True_With_Both_Branches()
    {
        // Arrange
        var thenVariable = new Variable<string>("thenVar", "initial", "thenVar");
        var elseVariable = new Variable<string>("elseVar", "initial", "elseVar");
        
        var thenActivity = new SetVariable<string>(thenVariable, new Input<string>("then_executed"));
        var elseActivity = new SetVariable<string>(elseVariable, new Input<string>("else_executed"));
        
        var ifActivity = new If(() => true)
        {
            Then = thenActivity,
            Else = elseActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_To_False_When_Condition_Is_False_With_Both_Branches()
    {
        // Arrange
        var thenVariable = new Variable<string>("thenVar", "initial", "thenVar");
        var elseVariable = new Variable<string>("elseVar", "initial", "elseVar");
        
        var thenActivity = new SetVariable<string>(thenVariable, new Input<string>("then_executed"));
        var elseActivity = new SetVariable<string>(elseVariable, new Input<string>("else_executed"));
        
        var ifActivity = new If(() => false)
        {
            Then = thenActivity,
            Else = elseActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.False(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_With_Missing_Else_Branch_When_Condition_Is_True()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var thenActivity = new SetVariable<string>(variable, new Input<string>("then_executed"));
        
        var ifActivity = new If(() => true)
        {
            Then = thenActivity
            // Else is intentionally null
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_With_Missing_Else_Branch_When_Condition_Is_False()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var thenActivity = new SetVariable<string>(variable, new Input<string>("then_executed"));
        
        var ifActivity = new If(() => false)
        {
            Then = thenActivity
            // Else is intentionally null
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.False(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_With_Missing_Then_Branch_When_Condition_Is_True()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var elseActivity = new SetVariable<string>(variable, new Input<string>("else_executed"));
        
        var ifActivity = new If(() => true)
        {
            // Then is intentionally null
            Else = elseActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }

    [Fact]
    public async Task Should_Set_Result_With_Missing_Then_Branch_When_Condition_Is_False()
    {
        // Arrange
        var variable = new Variable<string>("testVar", "initial", "testVar");
        var elseActivity = new SetVariable<string>(variable, new Input<string>("else_executed"));
        
        var ifActivity = new If(() => false)
        {
            // Then is intentionally null
            Else = elseActivity
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.False(resultValue);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Both_Branches_Are_Missing_And_Condition_Is_True()
    {
        // Arrange
        var ifActivity = new If(() => true)
        {
            // Both Then and Else are intentionally null
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await ActivityTestHelper.ExecuteActivityAsync(ifActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Both_Branches_Are_Missing_And_Condition_Is_False()
    {
        // Arrange
        var ifActivity = new If(() => false)
        {
            // Both Then and Else are intentionally null
        };

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await ActivityTestHelper.ExecuteActivityAsync(ifActivity));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task Should_Set_Result_When_Both_Branches_Are_Missing()
    {
        // Arrange
        var ifActivity = new If(() => true)
        {
            // Both Then and Else are intentionally null
        };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetExecutionOutput(_ => ifActivity.Result)!;
        Assert.True(resultValue);
    }
}
