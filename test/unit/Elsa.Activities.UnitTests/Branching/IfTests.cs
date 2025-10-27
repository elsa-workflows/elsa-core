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

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
