using Elsa.Workflows.Core.Attributes;
using System.Text.Json.Serialization;

namespace Elsa.Http.Models;

public record HttpEndpointBookmarkPayload
{
    private readonly string _path = default!;
    private readonly string _method = default!;

    [JsonConstructor]
    public HttpEndpointBookmarkPayload()
    {
    }

    public HttpEndpointBookmarkPayload(string path, string method,
        bool? authorize = default, string? policy = default)
    {
        Path = path;
        Method = method;
        Authorize = authorize;
        Policy = policy;
    }

    public string Path
    {
        get => _path;
        init => _path = value.ToLowerInvariant();
    }

    public string Method
    {
        get => _method;
        init => _method = value.ToLowerInvariant();
    }

    [ExcludeFromHash]
    public string? Policy { get; set; }

    [ExcludeFromHash]
    public bool? Authorize { get; set; }
}