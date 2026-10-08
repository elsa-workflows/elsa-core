using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="End"/> activity.
/// </summary>
public class EndTests
{
    [Fact(DisplayName = "End implements ITerminalNode interface")]
    public void End_ImplementsITerminalNode()
    {
        // Arrange
        var endActivity = new End();

        // Assert
        Assert.IsAssignableFrom<ITerminalNode>(endActivity);
    }

    [Fact(DisplayName = "End completes execution")]
    public async Task End_CompletesExecution()
    {
        // Arrange
        var endActivity = new End();
        var fixture = new ActivityTestFixture(endActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }
}
