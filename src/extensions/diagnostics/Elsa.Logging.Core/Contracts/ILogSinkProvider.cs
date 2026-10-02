namespace Elsa.Logging.Contracts;

/// <summary>
/// Represents a provider responsible for retrieving a collection of log sinks.
/// A log sink is an abstraction for a destination where log entries can be sent, such as a database or a file system.
/// </summary>
public interface ILogSinkProvider
{
    Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default);
}