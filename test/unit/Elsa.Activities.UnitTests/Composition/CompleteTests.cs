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

    [Fact(DisplayName = "Complete with constructor accepting string enumerable")]
    public void Complete_Constructor_AcceptsStringEnumerable()
    {
        // Arrange
        var expectedOutcomes = new[] { "Success", "Failed" };

        // Act
        var completeActivity = new Complete(expectedOutcomes);

        // Assert
        Assert.NotNull(completeActivity.Outcomes);
    }

    [Fact(DisplayName = "Complete with constructor accepting outcome function")]
    public void Complete_Constructor_AcceptsOutcomeFunction()
    {
        // Arrange & Act
        var completeActivity = new Complete(_ => "Success");

        // Assert
        Assert.NotNull(completeActivity.Outcomes);
    }

    [Fact(DisplayName = "Complete with constructor accepting outcomes collection function")]
    public void Complete_Constructor_AcceptsOutcomesCollectionFunction()
    {
        // Arrange & Act
        var completeActivity = new Complete(_ => ["Success", "Failed"]);

        // Assert
        Assert.NotNull(completeActivity.Outcomes);
    }
}
