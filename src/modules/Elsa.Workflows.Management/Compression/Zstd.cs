using System.Text;
using IronCompress;

namespace Elsa.Workflows.Management.Compression;

/// <summary>
/// Represents a ZSTD compression strategy.
/// </summary>
public class Zstd : ICompressionCodec
{
    private Iron Iron { get; set; } = new();

    /// <inheritdoc />
    public ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken = default)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var span = inputBytes.AsSpan();
        var result = Iron.Compress(Codec.Zstd, span);
        var compressedBytes = result.AsSpan();
        var compressedString = Convert.ToBase64String(compressedBytes);
        
        return new (compressedString);
    }

    /// <inheritdoc />
    public ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken = default)
    {
        var inputBytes = Convert.FromBase64String(input);
        var span = inputBytes.AsSpan();
        var result = Iron.Decompress(Codec.Zstd, span);
        var decompressedBytes = result.AsSpan();
        var decompressedString = Encoding.UTF8.GetString(decompressedBytes);
        
        return new (decompressedString);
    }
}