using System.IO.Compression;
using System.Text;

namespace Elsa.Workflows.Management.Compression;

/// <summary>
/// Represents a GZip compression strategy.
/// </summary>
public class GZip : ICompressionCodec
{
    /// <inheritdoc />
    public async ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var output = new MemoryStream();
        await using var compressionStream = new GZipStream(output, CompressionMode.Compress); 
        await compressionStream.WriteAsync(inputBytes, 0, inputBytes.Length, cancellationToken);
        await compressionStream.FlushAsync(cancellationToken);

        return Convert.ToBase64String(output.ToArray());
    }

    /// <inheritdoc />
    public async ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken)
    {
        var inputBytes = Convert.FromBase64String(input);
        using var inputMemoryStream = new MemoryStream(inputBytes);
        await using var decompressionStream = new GZipStream(inputMemoryStream, CompressionMode.Decompress);
        using var outputMemoryStream = new MemoryStream();
        await decompressionStream.CopyToAsync(outputMemoryStream, cancellationToken);
        var decompressedBytes = outputMemoryStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }
}