using System.Collections.Generic;
using System.Net;

namespace Elsa.Activities.Http.Models
{
    public class HttpResponseModel
    {
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, string[]> Headers { get; set; } = new();
    }
}