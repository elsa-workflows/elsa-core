using Elsa.Common;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a store of log records.
/// </summary>
public interface ILogRecordStore<in T> where T : ILogRecord
{
    /// <summary>
    /// Adds or updates the specified set oflog record objects in the persistence store.
    /// </summary>
    /// <remarks>
    /// If a record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveManyAsync(IEnumerable<T> records, CancellationToken cancellationToken = default);
}