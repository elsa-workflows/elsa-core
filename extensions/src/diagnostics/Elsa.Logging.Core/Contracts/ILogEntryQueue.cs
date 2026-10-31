using Elsa.Logging.Models;

namespace Elsa.Logging.Contracts;

/// <summary>
/// Represents a queue interface for managing log entry instructions.
/// Provides methods to enqueue log entry instructions and dequeue them asynchronously.
/// </summary>
public interface ILogEntryQueue
{
    /// <summary>
    /// Enqueues a log entry instruction asynchronously into the log entry queue.
    /// </summary>
    /// <param name="instruction">
    /// The log entry instruction to be added to the queue. This includes details like sink names, category,
    /// log level, message, arguments, and attributes for structured logging.
    /// </param>
    ValueTask EnqueueAsync(LogEntryInstruction instruction);

    /// <summary>
    /// Retrieves log entry instructions asynchronously from the log entry queue.
    /// </summary>
    /// <returns>
    /// An asynchronous enumerable sequence of log entry instructions.
    /// Each instruction contains details such as sink names, category, log level, message,
    /// arguments, and attributes for structured logging.
    /// </returns>
    IAsyncEnumerable<LogEntryInstruction> DequeueAsync();
}
