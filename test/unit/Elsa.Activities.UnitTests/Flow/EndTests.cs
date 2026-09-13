using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="End"/> activity.
/// </summary>
public class EndTests
{
    [Test]
    [DisplayName("End implements ITerminalNode interface")]
    public async Task End_ImplementsITerminalNode()
    {
        // Arrange
        var endActivity = new End();

        // Assert
        await Assert.That(endActivity).IsAssignableTo<ITerminalNode>();
    }

    [Test]
    [DisplayName("End completes execution")]
    public async Task End_CompletesExecution()
    {
        // Arrange
        var endActivity = new End();
        var fixture = new ActivityTestFixture(endActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }
}