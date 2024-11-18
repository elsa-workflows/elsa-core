using System.Text;
using Elsa.Helpers;
using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;

namespace Elsa.Http.DownloadableContentHandlers;

/// <summary>
/// Handles content that represents a downloadable string file.
/// </summary>
public class StringDownloadableContentHandler : DownloadableContentHandlerBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is string;

    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        var stream = StreamHelpers.RecyclableMemoryStreamManager.GetStream(nameof(Elsa.Http.DownloadableContentHandlers.StringDownloadableContentHandler.GetDownloadable), Encoding.UTF8.GetBytes((string)context.Content));
        return new(stream, "file.txt", "text/plain");
        // Returned streams are not disposed by the caller. Potential memory leak?! Dowloadable should implement IDisposable and dispose the stream.
    }
}
