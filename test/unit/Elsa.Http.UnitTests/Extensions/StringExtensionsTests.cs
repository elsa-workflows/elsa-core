using Elsa.Extensions;

namespace Elsa.Http.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Test]
    [Arguments("/api/users", "/api/users")]
    [Arguments("api/users", "/api/users")]
    [Arguments("/api/users/", "/api/users")]
    [Arguments("api/users/", "/api/users")]
    public async Task NormalizeRoute_VariousInputs_ReturnsNormalizedPath(string inputPath, string expectedPath)
    {
        // Act
        var normalizedPath = inputPath.NormalizeRoute();

        // Assert
        await Assert.That(normalizedPath).IsEqualTo(expectedPath);
    }
}
