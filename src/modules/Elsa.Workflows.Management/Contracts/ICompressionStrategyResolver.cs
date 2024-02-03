namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Resolves a <see cref="ICompressionStrategy"/> from its name.
/// </summary>
public interface ICompressionStrategyResolver
{
    /// <summary>
    /// Resolves a <see cref="ICompressionStrategy"/> from its name.
    /// </summary>
    ICompressionStrategy Resolve(string name);
}