using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Composition;

/// <summary>
/// Unit tests for the <see cref="Complete"/> activity.
/// </summary>
public class CompleteTests
{
    [Fact(DisplayName = "Complete implements ITerminalNode interface")]
    public void Complete_ImplementsITerminalNode()
    {
        // Arrange
        var completeActivity = new Complete();

        // Assert
        Assert.IsAssignableFrom<ITerminalNode>(completeActivity);
    }

    [Fact(DisplayName = "Complete completes execution")]
    public async Task Complete_CompletesExecution()
    {
        // Arrange
        var completeActivity = new Complete();
        var fixture = new ActivityTestFixture(completeActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }
}
