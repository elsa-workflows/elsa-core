using Elsa.Extensions;

namespace Elsa.Http.Models;

public class HttpRequestHeaders : Dictionary<string, string[]>
{
    public string? ContentType => this.GetValue("content-type")?[0];
}

public class HttpResponseHeaders : Dictionary<string, string[]>
{
    public string? ContentType => this.GetValue("content-type")?[0];
}