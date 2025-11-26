using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.UnitTests.Primitives;

/// <summary>
/// Unit tests for the <see cref="Finish"/> activity.
/// </summary>
public class FinishTests
{
    [Fact(DisplayName = "Finish completes successfully")]
    public async Task Should_Complete_Successfully()
    {
        // Arrange
        var finish = new Finish();

        // Act
        var context = await ExecuteAsync(finish);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Fact(DisplayName = "Finish transitions workflow to Finished substatus")]
    public async Task Should_Transition_Workflow_To_Finished()
    {
        // Arrange
        var finish = new Finish();

        // Act
        var context = await ExecuteAsync(finish);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, context.WorkflowExecutionContext.SubStatus);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }
}
