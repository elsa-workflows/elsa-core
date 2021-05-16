using System;
using System.Collections.Generic;

namespace Elsa.Activities.Http.Models
{
    public record HttpRequestModel(Uri Path, string Method, IDictionary<string, string> QueryString, IDictionary<string, string> Headers, object? Body = default)
    {
        public T GetBody<T>() => (T) Body!;
    }
}