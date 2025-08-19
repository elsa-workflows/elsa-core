namespace Elsa.Workflows.Contracts;

/// <summary>
/// Resolves logical sink names to ILogSink implementations.
/// </summary>
public interface ILogSinkResolver
{
    /// <summary>
    /// Resolves the specified sink names to ILogSink implementations.
    /// </summary>
    /// <param name="sinkNames">The logical sink names to resolve.</param>
    /// <returns>A collection of resolved log sinks.</returns>
    IEnumerable<ILogSink> Resolve(IEnumerable<string> sinkNames);
    
    /// <summary>
    /// Resolves a single sink name to an ILogSink implementation.
    /// </summary>
    /// <param name="sinkName">The logical sink name to resolve.</param>
    /// <returns>The resolved log sink, or null if not found.</returns>
    ILogSink? Resolve(string sinkName);
}