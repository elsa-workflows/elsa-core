using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;

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

    [Theory]
    [InlineData(true, true, false)] // condition true, has then branch, no else branch
    [InlineData(false, false, true)] // condition false, no then branch, has else branch
    public async Task Should_Set_Result_Correctly_With_Branch_Configuration(bool conditionValue, bool hasThenBranch, bool hasElseBranch)
    {
        // Arrange
        var ifActivity = new If(() => conditionValue);
        
        if (hasThenBranch)
        {
            // Using a simple WriteLine activity to avoid variable complexity
            ifActivity.Then = new WriteLine(new Input<string>("then executed"));
        }
        
        if (hasElseBranch)
        {
            ifActivity.Else = new WriteLine(new Input<string>("else executed"));
        }

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.Equal(conditionValue, resultValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Not_Throw_When_No_Branches_Are_Present(bool conditionValue)
    {
        // Arrange
        var ifActivity = new If(() => conditionValue);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(ifActivity));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Not_Throw_When_Only_Then_Branch_Is_Present(bool conditionValue)
    {
        // Arrange
        var ifActivity = new If(() => conditionValue)
        {
            Then = new WriteLine(new Input<string>("then branch"))
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
            Else = new WriteLine(new Input<string>("else branch"))
        };

        // Act
        var context = await ExecuteAsync(ifActivity);

        // Assert
        var resultValue = (bool)context.GetActivityOutput(() => ifActivity.Result)!;
        Assert.Equal(conditionValue, resultValue);
    }
    
    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
