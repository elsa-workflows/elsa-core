using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Models
{
    public class HttpRequestHeaders : Dictionary<string, string>
    {
        public string ContentType => this["content-type"];
    }
}