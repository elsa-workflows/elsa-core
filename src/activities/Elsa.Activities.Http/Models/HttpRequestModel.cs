using System;
using System.Collections.Generic;

namespace Elsa.Activities.Http.Models
{
    public class HttpRequestModel
    {
        public Uri Path { get; set; } = default!;
        public string Method { get; set; } = default!;
        public IDictionary<string, StringValuesModel> QueryString { get; set; } = default!;
        public IDictionary<string, StringValuesModel> Headers { get; set; } = default!;
        public object? Body { get; set; }
        public T GetBody<T>() => (T)Body!;
    }
}