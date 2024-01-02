using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Http.Serialization;

namespace Elsa.Http.Models;

/// <summary>
/// Represents the headers of an HTTP message.
/// </summary>
[JsonConverter(typeof(HttpHeadersConverter))]
public class HttpHeaders : Dictionary<string, string[]>
{
    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string? ContentType => this.GetValue("content-type")?[0];
}