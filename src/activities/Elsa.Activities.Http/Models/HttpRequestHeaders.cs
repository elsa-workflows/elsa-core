using System.Collections.Generic;

namespace Elsa.Activities.Http.Models
{
    public class HttpRequestHeaders : Dictionary<string, string>
    {
        public string ContentType => this["content-type"];
    }
}