using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class PropertyEvaluationTests
{
    [Test]
    [Arguments(false)] // Initial state
    [Arguments(true)]  // After setting flag
    public async Task HasEvaluatedProperties_TracksState(bool shouldSetFlag)
    {
        // Arrange
        var context = await CreateContextAsync();
        if (shouldSetFlag)
            context.SetHasEvaluatedProperties();

        // Act
        var result = context.GetHasEvaluatedProperties();

        // Assert
        await Assert.That(result).IsEqualTo(shouldSetFlag);
    }
}