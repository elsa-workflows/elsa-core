using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// A content writer that writes content in the application/x-www-form-urlencoded format. 
/// </summary>
public class FormUrlEncodedHttpContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => ["application/x-www-form-urlencoded"];

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string? contentType = null) => new FormUrlEncodedContent(GetContentAsDictionary(content));

    private static IDictionary<string, string> GetContentAsDictionary(object content)
    {
        if (content is IDictionary<string, object> dictionary)
            return dictionary.ToDictionary(x => x.Key, x => x.Value.ToString() ?? string.Empty);

        if (content is string or JsonObject)
            return JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(content));

        var jsonElement = JsonSerializer.SerializeToElement(content);
        return jsonElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString());
    }
}