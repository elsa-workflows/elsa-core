using Elsa.Extensions;
using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Models;

namespace Elsa.Http.Parsers;

/// <summary>
/// Reads received file from the HTTP response, if any.
/// </summary>
public class FileHttpContentParser : IHttpContentParser
{
    /// <inheritdoc />
    // Lower priority than other parsers, so that they can be tried first.
    // If none of them can parse the content, this parser will be tried and interpret the content as a file.
    public int Priority => -100; 

    /// <inheritdoc />
    public bool GetSupportsContentType(HttpResponseParserContext context)
    {
        return true;
    }

    /// <inheritdoc />
    public Task<object> ReadAsync(HttpResponseParserContext context)
    {
        var stream = context.Content;
        var filename = context.Headers.GetFilename() ?? "file.dat";
        var contentType = context.ContentType;
        context.Headers.TryGetValue("ETag", out var eTag);
        var file = new HttpFile(stream, filename, contentType, eTag?.FirstOrDefault());
        return Task.FromResult<object>(file);
    }
}