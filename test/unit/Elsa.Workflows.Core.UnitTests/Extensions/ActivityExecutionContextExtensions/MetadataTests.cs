using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class MetadataTests
{
    [Test]
    public async Task SetExtensionsMetadata_StoresValue()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.SetExtensionsMetadata("testKey", "testValue");

        // Assert
        var metadata = context.GetExtensionsMetadata();
        await Assert.That(metadata).IsNotNull();
        await Assert.That(metadata["testKey"]).IsEqualTo("testValue");
    }

    [Test]
    public async Task GetExtensionsMetadata_ReturnsNull_WhenNotSet()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        var metadata = context.GetExtensionsMetadata();

        // Assert
        await Assert.That(metadata).IsNull();
    }

    [Test]
    public async Task SetExtensionsMetadata_UpdatesExistingValue()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.SetExtensionsMetadata("testKey", "value1");

        // Act
        context.SetExtensionsMetadata("testKey", "value2");

        // Assert
        var metadata = context.GetExtensionsMetadata();
        await Assert.That(metadata!["testKey"]).IsEqualTo("value2");
    }
}