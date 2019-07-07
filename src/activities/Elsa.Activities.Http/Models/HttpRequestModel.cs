using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Elsa.Activities.Http.Models
{
    public class HttpRequestModel
    {
        public Uri Path { get; set; }
        public string Method { get; set; }
        public IDictionary<string, StringValues> QueryString { get; set; }
        public IDictionary<string, StringValues> Headers { get; set; }
        public string Content { get; set; }
        public object ParsedContent { get; set; }
        public IDictionary<string, StringValues> Form { get; set; }
    }
}