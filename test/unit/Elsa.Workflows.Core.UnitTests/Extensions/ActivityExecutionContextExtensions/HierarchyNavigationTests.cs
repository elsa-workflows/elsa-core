using Elsa.Extensions;
using Elsa.Workflows.Activities;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class HierarchyNavigationTests
{

    [Theory]
    [InlineData("GetAncestors")]
    [InlineData("GetDescendants")]
    public async Task HierarchyNavigation_ReturnsEmpty_WhenNoRelatives(string methodName)
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var result = methodName switch
        {
            "GetAncestors" => context.GetAncestors().ToList(),
            "GetDescendants" => context.GetDescendants().ToList(),
            _ => throw new ArgumentException($"Unknown method: {methodName}")
        };

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindParent_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var parent = context.FindParent(x => x.Activity is Sequence);

        // Assert
        Assert.Null(parent);
    }
}
