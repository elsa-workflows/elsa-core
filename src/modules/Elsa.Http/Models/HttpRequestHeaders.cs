using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Http.Serialization;

namespace Elsa.Http.Models;

/// <summary>
/// Represents the headers of an HTTP request.
/// </summary>
[JsonConverter(typeof(HttpRequestHeadersConverter))]
public class HttpRequestHeaders : Dictionary<string, string[]>
{
    /// <summary>
    /// Gets the content type of the request.
    /// </summary>
    public string? ContentType => this.GetValue("content-type")?[0];
}