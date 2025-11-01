using System.Text;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// HTTP content based on a string that preserves the provided media type as-is (no automatic charset parameter).
/// Uses a pre-encoded byte array to guarantee Content-Length == bytes written.
/// </summary>
public sealed class RawStringContent : ByteArrayContent
{
    public RawStringContent(string content, Encoding encoding, string mediaType)
        : base(GetBytes(content, encoding))
    {
        // Set media type exactly as provided, without charset parameter.
        Headers.ContentType = new(mediaType);
    }

    private static byte[] GetBytes(string content, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(encoding);

        // Ensure we don't include a BOM/preamble. Most Encoding instances can expose a preamble.
        // If encoding has a preamble, we must not include it, so use GetBytes directly (preamble is only returned by GetPreamble()).
        // For safety, if the encoding instance is a BOM-producing variant (e.g., new UTF8Encoding(true)),
        // GetBytes does not include the BOM; only GetPreamble() would. So this is safe.
        return encoding.GetBytes(content);
    }
}