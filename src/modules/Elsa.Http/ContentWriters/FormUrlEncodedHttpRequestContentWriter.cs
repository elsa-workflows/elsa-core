using System.Text.Json;
using Elsa.Http.Constants;

namespace Elsa.Http.ContentWriters;

public class FormUrlEncodedHttpRequestContentWriter : IHttpRequestContentWriter
{
    private List<string> SupportedContentTypes = new() {MimeTypes.ApplicationWwwFormUrlEncoded};

    public bool SupportsContentType(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }
    
    public HttpContent GetContent<T>( T content, string? contentType = null)
    {
        return new FormUrlEncodedContent(GetContentAsDictionary(content));
    }
    
    private Dictionary<string, string> GetContentAsDictionary<TType>(TType body)
    {
        return body is string || body is JsonObject ?
            JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(body)) :
            (Dictionary<string, string>)Convert.ChangeType(body, typeof(Dictionary<string, string>));
    }
}