namespace Elsa.Http.Parsers;

public interface IHttpResponseContentReader
{
    string Name { get; }
    int Priority { get; }
    bool GetSupportsContentType(string contentType);
    Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken);
}