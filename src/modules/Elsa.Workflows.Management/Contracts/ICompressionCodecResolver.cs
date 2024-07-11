namespace Elsa.Workflows.Management;

/// <summary>
/// Resolves a <see cref="ICompressionCodec"/> from its name.
/// </summary>
public interface ICompressionCodecResolver
{
    /// <summary>
    /// Resolves a <see cref="ICompressionCodec"/> from its name.
    /// </summary>
    ICompressionCodec Resolve(string name);
}