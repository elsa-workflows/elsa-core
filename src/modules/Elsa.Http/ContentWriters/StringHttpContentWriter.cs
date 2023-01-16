using System.Text;
using System.Text.Json;
using Elsa.Http.Constants;

namespace Elsa.Http.ContentWriters;

public class StringHttpContentWriter : IHttpContentWriter
{
    private List<string> SupportedContentTypes = new() {MimeTypes.ApplicationJson, MimeTypes.ApplicationXml};

    public bool SupportsContentType(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }

    public HttpContent GetContent<T>(T content, string? contentType = null)
    {
        var serializedContent = JsonSerializer.Serialize(content);
        return new StringContent(serializedContent, Encoding. UTF8, contentType);
    }
}