namespace Elsa.Http.ContentWriters;

public interface IHttpRequestContentWriter
{
    bool SupportsContentType(string contentType);
    HttpContent GetContent<T>(T content, string? contentType = null);
}