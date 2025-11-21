using System.Text;
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
    
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> produces correct content length without BOM.
    /// </summary>
    [Fact]
    public async Task JsonContentFactory_ShouldProduceCorrectContentLength()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"test\": \"value\"}";
        var factory = new JsonContentFactory();
        
        // Act
        var httpContent = factory.CreateHttpContent(content, contentType);
        var bytes = await httpContent.ReadAsByteArrayAsync();
        
        // Assert
        // Content length should match the actual bytes (no BOM)
        Assert.Equal(Encoding.UTF8.GetByteCount(content), bytes.Length);
        Assert.Equal(httpContent.Headers.ContentLength, bytes.Length);
        
        // Verify no BOM is present (BOM would be EF-BB-BF at start)
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }
    
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> handles multi-byte UTF-8 characters correctly.
    /// </summary>
    [Fact]
    public async Task JsonContentFactory_ShouldHandleMultiByteCharacters()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"emoji\": \"😀\", \"chinese\": \"你好\"}"; // Contains multi-byte UTF-8 characters
        var factory = new JsonContentFactory();
        
        // Act
        var httpContent = factory.CreateHttpContent(content, contentType);
        var bytes = await httpContent.ReadAsByteArrayAsync();
        
        // Assert
        // Byte count should be greater than character count due to multi-byte characters
        Assert.True(bytes.Length > content.Length);
        // Content length should match the actual UTF-8 byte count
        Assert.Equal(Encoding.UTF8.GetByteCount(content), bytes.Length);
        Assert.Equal(httpContent.Headers.ContentLength, bytes.Length);
    }
}