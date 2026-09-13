using System.Net.Http.Headers;
using System.Text;
using Elsa.Http.ContentWriters;

namespace Elsa.Http.UnitTests.ContentWriters;

/// <summary>
/// Tests for the <see cref="RawStringContent"/> class.
/// </summary>
public class RawStringContentTests
{
    /// <summary>
    /// Tests that the content type is set exactly as provided without appending charset information.
    /// </summary>
    [Test]
    public async Task ContentType_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/xml";
        const string content = "<root>test</root>";
        
        // Act
        using var rawContent = new RawStringContent(content, Encoding.UTF8, contentType);
        
        // Assert
        await Assert.That(rawContent.Headers.ContentType?.MediaType).IsEqualTo(contentType);
        await Assert.That(rawContent.Headers.ContentType?.CharSet).IsNull();
    }
}
