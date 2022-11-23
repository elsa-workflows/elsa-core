using System.Text.Json.Serialization;

namespace Elsa.Http.Models;

public record HttpRequestModel(
    Uri RequestUri,
    string Path,
    string Method,
    IDictionary<string, string> QueryString,
    IDictionary<string, object> RouteValues,
    IDictionary<string, string> Headers
)
{
    
    /// <summary>
    /// Constructor used for deserialization.
    /// </summary>
    [JsonConstructor]
    public HttpRequestModel() : this(default!, default!, default!, default!, default!, default!)
    {
    }
}