using Elsa.Testing.Shared;
using Elsa.Workflows;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetNameTests
{
    [Test]
    [DisplayName("SetName sets workflow name and completes successfully")]
    public async Task Should_Set_Workflow_Name_And_Complete()
    {
        // Arrange
        const string name = "My Workflow";

        // Act
        var context = await ExecuteAsync(name);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Name).IsEqualTo(name);
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("SetName handles various name values")]
    [Arguments("Simple Name")]
    [Arguments("Name-With-Dashes")]
    [Arguments("Name_With_Underscores")]
    [Arguments("Name.With.Dots")]
    [Arguments("123 Numeric Name")]
    [Arguments("")]
    public async Task Should_Handle_Various_Name_Values(string name)
    {
        // Act
        var context = await ExecuteAsync(name);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Name).IsEqualTo(name);
    }

    [Test]
    [DisplayName("SetName handles null value")]
    public async Task Should_Handle_Null_Value()
    {
        // Act
        var context = await ExecuteAsync(null!);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Name).IsNull();
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