using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.UnitTests.Looping;

/// <summary>
/// Unit tests for the <see cref="Break"/> activity.
/// </summary>
public class BreakTests
{
    [Fact(DisplayName = "Break implements ITerminalNode interface")]
    public void Break_ImplementsITerminalNode()
    {
        // Arrange
        var breakActivity = new Break();

        // Assert
        Assert.IsAssignableFrom<ITerminalNode>(breakActivity);
    }

    [Fact(DisplayName = "Break completes execution")]
    public async Task Break_CompletesExecution()
    {
        // Arrange
        var breakActivity = new Break();
        var fixture = new ActivityTestFixture(breakActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }
}
