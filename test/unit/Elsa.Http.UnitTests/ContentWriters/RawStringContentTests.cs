using System.Net.Http.Headers;
using System.Text;
using Elsa.Http.ContentWriters;
using Xunit;

namespace Elsa.Http.UnitTests.ContentWriters;

/// <summary>
/// Tests for the <see cref="RawStringContent"/> class.
/// </summary>
public class RawStringContentTests
{
    /// <summary>
    /// Tests that the content type is set exactly as provided without appending charset information.
    /// </summary>
    [Fact]
    public void ContentType_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/xml";
        const string content = "<root>test</root>";
        
        // Act
        var rawContent = new RawStringContent(content, Encoding.UTF8, contentType);
        
        // Assert
        Assert.Equal(contentType, rawContent.Headers.ContentType?.MediaType);
        Assert.Null(rawContent.Headers.ContentType?.CharSet);
    }
}