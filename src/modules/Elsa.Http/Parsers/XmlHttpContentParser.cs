using System.Xml.Serialization;
using Elsa.Http.Contracts;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads application/xml and text/xml content type streams.
/// </summary>
public class XmlHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsContentType(string contentType) => contentType.Contains("xml", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public async Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var xml = await reader.ReadToEndAsync();

        if (returnType == null || returnType == typeof(string))
            return xml;
        
        var serializer = new XmlSerializer(returnType);
        return serializer.Deserialize(reader)!;
    }
}