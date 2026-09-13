using System.Text;
using Elsa.Http.ContentWriters;

namespace Elsa.Http.UnitTests.ContentWriters;

/// <summary>
/// Tests for the <see cref="IHttpContentFactory"/> implementations.
/// </summary>
public class ContentFactoryTests
{
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Test]
    public async Task JsonContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"test\": \"value\"}";
        var factory = new JsonContentFactory();
        
        // Act
        using var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        await Assert.That(httpContent.Headers.ContentType?.MediaType).IsEqualTo(contentType);
        await Assert.That(httpContent.Headers.ContentType?.CharSet).IsNull();
    }
    
    /// <summary>
    /// Tests that <see cref="XmlContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Test]
    public async Task XmlContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/xml";
        const string content = "<root>test</root>";
        var factory = new XmlContentFactory();
        
        // Act
        using var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        await Assert.That(httpContent.Headers.ContentType?.MediaType).IsEqualTo(contentType);
        await Assert.That(httpContent.Headers.ContentType?.CharSet).IsNull();
    }
    
    /// <summary>
    /// Tests that <see cref="TextContentFactory"/> doesn't append charset to content type.
    /// </summary>
    [Test]
    public async Task TextContentFactory_ShouldNotAppendCharset()
    {
        // Arrange
        const string contentType = "text/html";
        const string content = "<html><body>test</body></html>";
        var factory = new TextContentFactory();
        
        // Act
        using var httpContent = factory.CreateHttpContent(content, contentType);
        
        // Assert
        await Assert.That(httpContent.Headers.ContentType?.MediaType).IsEqualTo(contentType);
        await Assert.That(httpContent.Headers.ContentType?.CharSet).IsNull();
    }
    
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> produces correct content length without BOM.
    /// </summary>
    [Test]
    public async Task JsonContentFactory_ShouldProduceCorrectContentLength()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"test\": \"value\"}";
        var factory = new JsonContentFactory();
        
        // Act
        using var httpContent = factory.CreateHttpContent(content, contentType);
        var bytes = await httpContent.ReadAsByteArrayAsync();
        
        // Assert
        // Content length should match the actual bytes (no BOM)
        await Assert.That(bytes.Length).IsEqualTo(Encoding.UTF8.GetByteCount(content));
        await Assert.That(httpContent.Headers.ContentLength).IsEqualTo(bytes.LongLength);

        // Verify no BOM is present (BOM would be EF-BB-BF at start)
        await Assert.That(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF).IsFalse();
    }
    
    /// <summary>
    /// Tests that <see cref="JsonContentFactory"/> handles multi-byte UTF-8 characters correctly.
    /// </summary>
    [Test]
    public async Task JsonContentFactory_ShouldHandleMultiByteCharacters()
    {
        // Arrange
        const string contentType = "application/json";
        const string content = "{\"emoji\": \"😀\", \"chinese\": \"你好\"}"; // Contains multi-byte UTF-8 characters
        var factory = new JsonContentFactory();
        
        // Act
        using var httpContent = factory.CreateHttpContent(content, contentType);
        var bytes = await httpContent.ReadAsByteArrayAsync();
        
        // Assert
        // Byte count should be greater than character count due to multi-byte characters
        await Assert.That(bytes.Length > content.Length).IsTrue();
        // Content length should match the actual UTF-8 byte count
        await Assert.That(bytes.Length).IsEqualTo(Encoding.UTF8.GetByteCount(content));
        await Assert.That(httpContent.Headers.ContentLength).IsEqualTo(bytes.LongLength);
    }
}
