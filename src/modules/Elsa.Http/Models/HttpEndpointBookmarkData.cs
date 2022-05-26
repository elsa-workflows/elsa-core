namespace Elsa.Http.Models;

public record HttpEndpointBookmarkData
{
    private readonly string _path = default!;
    private readonly string _method = default!;

    public HttpEndpointBookmarkData(string path, string method)
    {
        Path = path;
        Method = method;
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
}