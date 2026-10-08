using Microsoft.AspNetCore.Http;

namespace Elsa.Http;

public class HttpEndpointOptions
{
    public string Path { get; set; } = null!;
    public ICollection<string> Methods { get; set; } = [HttpMethods.Get];
    public bool Authorize { get; set; }
    public string? Policy { get; set; }
    public TimeSpan? RequestTimeout { get; set; }
    public long? RequestSizeLimit { get; set; }
}