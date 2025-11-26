using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetNameTests
{
    [Fact(DisplayName = "SetName sets workflow name and completes successfully")]
    public async Task Should_Set_Workflow_Name_And_Complete()
    {
        // Arrange
        const string name = "My Workflow";

        // Act
        var context = await ExecuteAsync(name);

        // Assert
        Assert.Equal(name, context.WorkflowExecutionContext.Name);
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Theory(DisplayName = "SetName handles various name values")]
    [InlineData("Simple Name")]
    [InlineData("Name-With-Dashes")]
    [InlineData("Name_With_Underscores")]
    [InlineData("Name.With.Dots")]
    [InlineData("123 Numeric Name")]
    [InlineData("")]
    public async Task Should_Handle_Various_Name_Values(string name)
    {
        // Act
        var context = await ExecuteAsync(name);

        // Assert
        Assert.Equal(name, context.WorkflowExecutionContext.Name);
    }

    [Fact(DisplayName = "SetName handles null value")]
    public async Task Should_Handle_Null_Value()
    {
        // Act
        var context = await ExecuteAsync(null!);

        // Assert
        Assert.Null(context.WorkflowExecutionContext.Name);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(string name)
    {
        var setName = new SetName
        {
            Value = new(name)
        };
        return await new ActivityTestFixture(setName).ExecuteAsync();
    }
}
