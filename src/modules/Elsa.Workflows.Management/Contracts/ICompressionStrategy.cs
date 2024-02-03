namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Represents a compression strategy.
/// </summary>
public interface ICompressionStrategy
{
    /// <summary>
    /// Compresses the input.
    /// </summary>
    ValueTask<string> CompressAsync(string input, CancellationToken cancellationToken);
    
    /// <summary>
    /// Decompresses the input.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<string> DecompressAsync(string input, CancellationToken cancellationToken);
}