using Elsa.Helpers;
using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;

namespace Elsa.Http.DownloadableContentHandlers;

/// <summary>
/// Handles content that represents a downloadable binary file.
/// </summary>
public class BinaryDownloadableContentHandler : DownloadableContentHandlerBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is byte[];

    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        var bytes = (byte[]) context.Content;
        var stream = StreamHelpers.RecyclableMemoryStreamManager.GetStream(nameof(Elsa.Http.DownloadableContentHandlers.BinaryDownloadableContentHandler.GetDownloadable), bytes);
        var fileName = "file.bin";
        var contentType = "application/octet-stream";
        return new(stream, fileName, contentType);
        // Returned streams are not disposed by the caller. Potential memory leak?! Dowloadable should implement IDisposable and dispose the stream.
    }
}