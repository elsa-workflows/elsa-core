using Elsa.Testing.Shared;
using Elsa.Workflows;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Primitives;

/// <summary>
/// Unit tests for the <see cref="Finish"/> activity.
/// </summary>
public class FinishTests
{
    [Test]
    [DisplayName("Finish implements ITerminalNode interface")]
    public async Task Finish_ImplementsITerminalNode()
    {
        // Arrange
        var finish = new Finish();

        // Assert
        await Assert.That(finish).IsAssignableTo<ITerminalNode>();
    }

    [Test]
    [DisplayName("Finish completes successfully")]
    public async Task Should_Complete_Successfully()
    {
        // Arrange
        var finish = new Finish();

        // Act
        var context = await ExecuteAsync(finish);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("Finish transitions workflow to Finished substatus")]
    public async Task Should_Transition_Workflow_To_Finished()
    {
        // Arrange
        var finish = new Finish();

        // Act
        var context = await ExecuteAsync(finish);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}