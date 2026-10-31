using Elsa.Extensions;

namespace Elsa.Http.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("/api/users", "/api/users")]
    [InlineData("api/users", "/api/users")]
    [InlineData("/api/users/", "/api/users")]
    [InlineData("api/users/", "/api/users")]
    public void NormalizeRoute_VariousInputs_ReturnsNormalizedPath(string inputPath, string expectedPath)
    {
        // Act
        var normalizedPath = inputPath.NormalizeRoute();

        // Assert
        Assert.Equal(expectedPath, normalizedPath);
    }
}
