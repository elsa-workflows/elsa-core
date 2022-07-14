using System.Collections.Generic;

namespace Elsa.Activities.Http.Models
{
    public record HttpRequestModel(string Path, string Method, IDictionary<string, string> QueryString, IDictionary<string, object> RouteValues, IDictionary<string, string> Headers, object? Body = default, string? RawBody = default)
    {
        public T GetBody<T>() => (T) Body!;
    }
}