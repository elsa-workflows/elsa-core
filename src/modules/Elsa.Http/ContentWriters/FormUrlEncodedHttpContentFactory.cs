using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// A content writer that writes content in the application/x-www-form-urlencoded format. 
/// </summary>
public class FormUrlEncodedHttpContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => new[] { "application/x-www-form-urlencoded" };

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string? contentType = null) => new FormUrlEncodedContent(GetContentAsDictionary(content));

    private static Dictionary<string, string> GetContentAsDictionary(object content) =>
        (content is string or JsonObject
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(content))
            : (Dictionary<string, string>)Convert.ChangeType(content, typeof(Dictionary<string, string>)))!;
}