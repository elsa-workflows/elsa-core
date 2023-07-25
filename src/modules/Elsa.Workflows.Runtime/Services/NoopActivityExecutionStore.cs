using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// An activity execution log store that does nothing.
/// </summary>
public class NoopActivityExecutionStore : IActivityExecutionStore
{
    /// <inheritdoc />
    public Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ActivityExecutionRecord>());

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<ActivityExecutionRecord>());

    /// <inheritdoc />
    public Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(0L);
}