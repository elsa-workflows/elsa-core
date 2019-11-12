using System;
using System.Collections.Generic;

namespace Elsa.Activities.Http.Models
{
    public class HttpRequestModel
    {
        public Uri Path { get; set; }
        public string Method { get; set; }
        public IDictionary<string, StringValuesModel> QueryString { get; set; }
        public IDictionary<string, StringValuesModel> Headers { get; set; }
        public object Body { get; set; }
    }
}