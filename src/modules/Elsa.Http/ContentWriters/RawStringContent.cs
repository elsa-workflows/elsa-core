using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// A <see cref="HttpContent"/> implementation that allows setting the content type without automatically appending charset information.
/// </summary>
public class RawStringContent : HttpContent
{
    private readonly string _content;
    private readonly Encoding _encoding;

    /// <summary>
    /// Creates a new instance of the <see cref="RawStringContent"/> class.
    /// </summary>
    /// <param name="content">The content to send.</param>
    /// <param name="encoding">The encoding to use when sending the content.</param>
    /// <param name="mediaType">The media type to use for the content.</param>
    public RawStringContent(string content, Encoding encoding, string mediaType)
    {
        _content = content;
        _encoding = encoding;
        
        // Set the media type exactly as provided without appending charset information
        Headers.ContentType = new MediaTypeHeaderValue(mediaType);
    }

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => 
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    /// <inheritdoc />
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(stream, _encoding, leaveOpen: true);
        await writer.WriteAsync(_content.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = _encoding.GetByteCount(_content);
        return true;
    }
}