using Elsa.Http.Contracts;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads application/xml and text/xml content type streams.
/// </summary>
public class PlainTextHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsContentType(string contentType) => contentType.Contains("text/plain", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public async Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var stringContent = await reader.ReadToEndAsync();
        return stringContent;
    }
}