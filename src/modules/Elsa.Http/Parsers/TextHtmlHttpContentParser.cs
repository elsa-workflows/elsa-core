using Elsa.Http.Contracts;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads text/html content type streams.
/// </summary>
// TODO: found a library to use a Html Content Parser and use a complexe object Type, until this, this class allow to accept request send using text/html content-type
public class TextHtmlHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsContentType(string contentType) => contentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public async Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var stringContent = await reader.ReadToEndAsync();
        return stringContent;
    }
}