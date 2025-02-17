using Elsa.Common.Contracts;

namespace Elsa.Workflows.Runtime.Contracts;

/// Represents a store of log records.
public interface ILogRecordStore<in T> where T : ILogRecord
{
    /// Adds or updates the specified set oflog record objects in the persistence store.
    /// <remarks>
    /// If a record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of log records to the store.
    /// </summary>
    Task AddManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default);
}