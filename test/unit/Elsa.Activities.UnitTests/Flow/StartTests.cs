using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="Start"/> activity.
/// </summary>
public class StartTests
{
    [Test]
    [DisplayName("Start implements IStartNode interface")]
    public async Task Start_ImplementsIStartNode()
    {
        // Arrange
        var startActivity = new Start();

        // Assert
        await Assert.That(startActivity).IsAssignableTo<IStartNode>();
    }

    [Test]
    [DisplayName("Start completes execution")]
    public async Task Start_CompletesExecution()
    {
        // Arrange
        var startActivity = new Start();
        var fixture = new ActivityTestFixture(startActivity);

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }
}