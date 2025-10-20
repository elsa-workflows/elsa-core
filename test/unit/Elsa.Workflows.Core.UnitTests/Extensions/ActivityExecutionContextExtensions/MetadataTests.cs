using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class MetadataTests
{
    [Fact]
    public async Task SetExtensionsMetadata_StoresValue()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.SetExtensionsMetadata("testKey", "testValue");

        // Assert
        var metadata = context.GetExtensionsMetadata();
        Assert.NotNull(metadata);
        Assert.Equal("testValue", metadata["testKey"]);
    }

    [Fact]
    public async Task GetExtensionsMetadata_ReturnsNull_WhenNotSet()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var metadata = context.GetExtensionsMetadata();

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public async Task SetExtensionsMetadata_UpdatesExistingValue()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.SetExtensionsMetadata("testKey", "value1");

        // Act
        context.SetExtensionsMetadata("testKey", "value2");

        // Assert
        var metadata = context.GetExtensionsMetadata();
        Assert.Equal("value2", metadata!["testKey"]);
    }
}
