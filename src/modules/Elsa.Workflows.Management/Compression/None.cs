namespace Elsa.Workflows.Management.Compression;

/// <summary>
/// Represents a compression strategy that does not compress or decompress the input.
/// </summary>
public class None : ICompressionCodec
{
    /// <inheritdoc />
    public ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken)
    {
        return new ValueTask<string>(input);
    }

    /// <inheritdoc />
    public ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken)
    {
        return new ValueTask<string>(input);
    }
}