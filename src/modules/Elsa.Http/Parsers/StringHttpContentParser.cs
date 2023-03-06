using Elsa.Expressions.Helpers;
using Elsa.Http.Contracts;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads any content type streams as a string and attempts to convert the string to the specified return type.
/// </summary>
public class StringHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    public int Priority => -10;

    /// <inheritdoc />
    public bool GetSupportsContentType(string contentType) => true;

    /// <inheritdoc />
    public async Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        return returnType == null ? text : text.ConvertTo(returnType)!;
    }
}