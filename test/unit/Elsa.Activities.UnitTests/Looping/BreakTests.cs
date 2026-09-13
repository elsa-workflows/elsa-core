using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Looping;

/// <summary>
/// Unit tests for the <see cref="Break"/> activity.
/// </summary>
public class BreakTests
{
    [Test]
    [DisplayName("Break implements ITerminalNode interface")]
    public async Task Break_ImplementsITerminalNode()
    {
        // Arrange
        var breakActivity = new Break();

        // Assert
        await Assert.That(breakActivity).IsAssignableTo<ITerminalNode>();
    }

    [Test]
    [DisplayName("Break completes execution")]
    public async Task Break_CompletesExecution()
    {
        // Arrange
        var breakActivity = new Break();
        var fixture = new ActivityTestFixture(breakActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }
}