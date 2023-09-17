using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Models;

namespace Elsa.Http.DownloadableContentHandlers;

/// <summary>
/// Handles content that represents a downloadable stream.
/// </summary>
public class StreamDownloadableContentHandler : DownloadableContentHandlerBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is Stream;

    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        var stream = (Stream) context.Content;
        var fileName = "file.bin";
        var contentType = "application/octet-stream";
        return new Downloadable(stream, fileName, contentType);
    }
}