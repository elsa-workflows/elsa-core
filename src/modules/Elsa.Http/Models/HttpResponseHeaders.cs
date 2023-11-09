using Elsa.Extensions;

namespace Elsa.Http.Models;

/// <summary>
/// Represents the headers of an HTTP response.
/// </summary>
public class HttpResponseHeaders : Dictionary<string, string[]>
{
    /// <summary>
    /// Gets the content type of the response.
    /// </summary>
    public string? ContentType => this.GetValue("content-type")?[0];
}