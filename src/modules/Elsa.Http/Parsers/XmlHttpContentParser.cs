using System.Xml.Serialization;
using Elsa.Http.Contexts;
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
    public bool GetSupportsContentType(HttpResponseParserContext context) => context.ContentType.Contains("xml", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc />
    public async Task<object> ReadAsync(HttpResponseParserContext context)
    {
        var content = context.Content;
        using var reader = new StreamReader(content, leaveOpen: true);
        var xml = await reader.ReadToEndAsync();
        var returnType = context.ReturnType;

        if (returnType == null || returnType == typeof(string))
            return xml;

        var serializer = new XmlSerializer(returnType);
        return serializer.Deserialize(reader)!;
    }
}