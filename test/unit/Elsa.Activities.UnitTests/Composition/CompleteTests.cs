using Elsa.Testing.Shared;
using Elsa.Workflows;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Composition;

/// <summary>
/// Unit tests for the <see cref="Complete"/> activity.
/// </summary>
public class CompleteTests
{
    [Test]
    [DisplayName("Complete implements ITerminalNode interface")]
    public async Task Complete_ImplementsITerminalNode()
    {
        // Arrange
        var completeActivity = new Complete();

        // Assert
        await Assert.That(completeActivity).IsAssignableTo<ITerminalNode>();
    }

    [Test]
    [DisplayName("Complete completes execution")]
    public async Task Complete_CompletesExecution()
    {
        // Arrange
        var completeActivity = new Complete();
        var fixture = new ActivityTestFixture(completeActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }
}