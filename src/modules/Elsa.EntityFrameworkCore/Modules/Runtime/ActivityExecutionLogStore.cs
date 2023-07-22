using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IActivityExecutionLogStore"/>.
/// </summary>
public class EFCoreActivityExecutionLogStore : IActivityExecutionLogStore
{
    private readonly EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCoreActivityExecutionLogStore"/> class.
    /// </summary>
    public EFCoreActivityExecutionLogStore(EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, default, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default) => await _store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);

    private IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}