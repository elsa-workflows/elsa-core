using System.Xml.Serialization;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads application/xml and text/xml content type streams.
/// </summary>
public class FormHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsContentType(string contentType) => contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public async Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken)
    {
        using var reader = new FormReader(content);

        return (await reader.ReadFormAsync(cancellationToken)).ToDictionary<KeyValuePair<string, StringValues>, string, object>(formField => formField.Key, formField => formField.Value[0]!); ;
    }
}