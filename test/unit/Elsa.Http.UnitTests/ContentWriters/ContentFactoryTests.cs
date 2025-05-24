using Elsa.Http.ContentWriters;
using Xunit;

namespace Elsa.Http.UnitTests.ContentWriters;

/// <summary>
/// Tests for the <see cref="IHttpContentFactory"/> implementations.
/// </summary>
public class ContentFactoryTests
{
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Fact]
    public void JsonContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"test\": \"value\"}";
        var factory = new JsonContentFactory();
        
        // Act
        var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        Assert.Equal(contentType, httpContent.Headers.ContentType?.MediaType);
        Assert.Null(httpContent.Headers.ContentType?.CharSet);
    }
    
    /// <summary>
    /// Tests that <see cref="XmlContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Fact]
    public void XmlContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/xml";
        const string content = "<root>test</root>";
        var factory = new XmlContentFactory();
        
        // Act
        var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        Assert.Equal(contentType, httpContent.Headers.ContentType?.MediaType);
        Assert.Null(httpContent.Headers.ContentType?.CharSet);
    }
    
    /// <summary>
    /// Tests that <see cref="TextContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Fact]
    public void TextContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/html";
        const string content = "<html><body>test</body></html>";
        var factory = new TextContentFactory();
        
        // Act
        var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        Assert.Equal(contentType, httpContent.Headers.ContentType?.MediaType);
        Assert.Null(httpContent.Headers.ContentType?.CharSet);
    }
}