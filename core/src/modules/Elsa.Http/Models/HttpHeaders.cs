using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Http.Serialization;

namespace Elsa.Http;

/// <summary>
/// Represents the headers of an HTTP message.
/// </summary>
[JsonConverter(typeof(HttpHeadersConverter))]
public class HttpHeaders : Dictionary<string, string[]>
{
    /// <inheritdoc />
    public HttpHeaders()
    {
    }

    /// <inheritdoc />
    public HttpHeaders(IDictionary<string, string[]> source)
    {
        foreach (var item in source)
            Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public HttpHeaders(HttpResponseHeaders source)
    {
        foreach (var item in source)
            Add(item.Key, item.Value.ToArray());
    }

    /// <inheritdoc />
    public HttpHeaders(HttpContentHeaders source)
    {
        foreach (var item in source)
            Add(item.Key, item.Value.ToArray());
    }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string? ContentType => this.GetValue("content-type")?[0];
}