using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class PropertyEvaluationTests
{
    [Theory]
    [InlineData(false)] // Initial state
    [InlineData(true)]  // After setting flag
    public async Task HasEvaluatedProperties_TracksState(bool shouldSetFlag)
    {
        // Arrange
        var context = await CreateContextAsync();
        if (shouldSetFlag)
            context.SetHasEvaluatedProperties();

        // Act
        var result = context.GetHasEvaluatedProperties();

        // Assert
        Assert.Equal(shouldSetFlag, result);
    }
}
