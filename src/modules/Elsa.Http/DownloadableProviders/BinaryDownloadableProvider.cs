using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Models;

namespace Elsa.Http.DownloadableProviders;

/// <summary>
/// Handles content that represents a downloadable binary file.
/// </summary>
public class BinaryDownloadableProvider : DownloadableProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is byte[];

    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        var bytes = (byte[]) context.Content;
        var stream = new MemoryStream(bytes);
        var fileName = "file.bin";
        var contentType = "application/octet-stream";
        return new Downloadable(stream, fileName, contentType);
    }
}