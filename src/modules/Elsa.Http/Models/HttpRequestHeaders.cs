using System.Collections.Generic;

namespace Elsa.Http.Models;

public class HttpRequestHeaders : Dictionary<string, string>
{
    public string ContentType => this["content-type"];
}