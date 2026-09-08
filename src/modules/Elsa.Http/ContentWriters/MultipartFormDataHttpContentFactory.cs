using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// A content writer that writes content in the multipart/form-data format.
/// </summary>
public class MultipartFormDataHttpContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => ["multipart/form-data"];

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string? contentType = null)
    {
        var multipartContent = new MultipartFormDataContent();

        foreach (var (key, value) in GetContentAsDictionary(content))
            multipartContent.Add(new StringContent(value), key);

        return multipartContent;
    }

    private static IDictionary<string, string> GetContentAsDictionary(object content)
    {
        if (content is IDictionary<string, object> dictionary)
            return dictionary.ToDictionary(x => x.Key, x => x.Value.ToString() ?? string.Empty);

        if (content is string or JsonObject)
            return JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(content))!;

        var jsonElement = JsonSerializer.SerializeToElement(content);
        return jsonElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString());
    }
}
