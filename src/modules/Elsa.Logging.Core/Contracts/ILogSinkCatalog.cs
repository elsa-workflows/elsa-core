namespace Elsa.Logging.Contracts;

/// <summary>
/// Represents a catalog for managing log sinks in the logging system.
/// Provides functionality to list all available log sinks and retrieve a specific log sink by its identifier.
/// </summary>
public interface ILogSinkCatalog
{
    /// <summary>
    /// Asynchronously retrieves a list of available log sinks.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A collection of <see cref="ILogSink"/> representing the available log sinks.
    /// </returns>
    Task<IEnumerable<ILogSink>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a specific log sink by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the log sink to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An instance of <see cref="ILogSink"/> representing the log sink if found; otherwise, null.
    /// </returns>
    Task<ILogSink?> GetAsync(string id, CancellationToken cancellationToken = default);
}