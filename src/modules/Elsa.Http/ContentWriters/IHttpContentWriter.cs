namespace Elsa.Http.ContentWriters;

public interface IHttpContentWriter
{
    bool SupportsContentType(string contentType);
    HttpContent GetContent<T>(T content, string? contentType = null);
}