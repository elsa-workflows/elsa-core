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
    
    /// <summary>
    /// Tests that the content type with parameters is preserved exactly as provided.
    /// </summary>
    [Fact]
    public void ContentType_WithParameters_ShouldPreserveParameters()
    {
        // Arrange
        const string contentType = "application/json; custom-param=value";
        var expectedMediaType = new MediaTypeHeaderValue(contentType);
        const string content = "{\"test\": \"value\"}";
        
        // Act
        var rawContent = new RawStringContent(content, Encoding.UTF8, contentType);
        
        // Assert
        Assert.Equal(expectedMediaType.MediaType, rawContent.Headers.ContentType?.MediaType);
        Assert.Equal(expectedMediaType.Parameters.Count(), rawContent.Headers.ContentType?.Parameters.Count());
        
        var expectedParam = expectedMediaType.Parameters.First();
        var actualParam = rawContent.Headers.ContentType?.Parameters.First();
        
        Assert.Equal(expectedParam.Name, actualParam?.Name);
        Assert.Equal(expectedParam.Value, actualParam?.Value);
    }
}