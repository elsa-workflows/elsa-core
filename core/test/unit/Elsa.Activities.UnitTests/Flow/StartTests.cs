using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="Start"/> activity.
/// </summary>
public class StartTests
{
    [Fact(DisplayName = "Start implements IStartNode interface")]
    public void Start_ImplementsIStartNode()
    {
        // Arrange
        var startActivity = new Start();

        // Assert
        Assert.IsAssignableFrom<IStartNode>(startActivity);
    }

    [Fact(DisplayName = "Start completes execution")]
    public async Task Start_CompletesExecution()
    {
        // Arrange
        var startActivity = new Start();
        var fixture = new ActivityTestFixture(startActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }
}
