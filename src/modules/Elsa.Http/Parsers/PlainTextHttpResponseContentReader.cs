namespace Elsa.Http.Parsers;

public class PlainTextHttpResponseContentReader : IHttpResponseContentReader
{
    public string Name => "Plain Text";
    public int Priority => -1;
    public bool GetSupportsContentType(string contentType) => true;
    public async Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken) => await response.Content.ReadAsStringAsync();
}