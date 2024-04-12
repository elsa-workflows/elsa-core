namespace Elsa.Workflows.Management;

/// <summary>
/// Represents a compression strategy.
/// </summary>
public interface ICompressionCodec
{
    /// <summary>
    /// Compresses the input.
    /// </summary>
    ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Decompresses the input.
    /// </summary>
    ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken = default);
}