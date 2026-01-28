using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// Stores activity execution records in memory.
/// </summary>
public class MemoryActivityExecutionStore : IActivityExecutionStore
{
    private readonly MemoryStore<ActivityExecutionRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryActivityExecutionStore"/> class.
    /// </summary>
    public MemoryActivityExecutionStore(MemoryStore<ActivityExecutionRecord> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        _store.AddMany(records, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, order, cancellationToken);
        return entities.Select(ActivityExecutionRecordSummary.FromRecord);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, cancellationToken);
        return entities.Select(ActivityExecutionRecordSummary.FromRecord);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = _store.Query(query => Filter(query, filter)).ToList();
        _store.DeleteMany(records, x => x.Id);
        return Task.FromResult(records.LongCount());
    }

    /// <inheritdoc />
    public Task<Page<ActivityExecutionRecord>> GetExecutionChainAsync(
        string activityExecutionId,
        bool includeCrossWorkflowChain = true,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var chain = new List<ActivityExecutionRecord>();
        var currentId = activityExecutionId;

        // Traverse the chain backwards from the specified record to the root
        while (currentId != null)
        {
            var record = _store.Query(query => query.Where(x => x.Id == currentId)).FirstOrDefault();
            if (record == null)
                break;

            chain.Add(record);

            // If not including cross-workflow chain and we hit a workflow boundary, stop
            if (!includeCrossWorkflowChain && record.SchedulingWorkflowInstanceId != null)
                break;

            currentId = record.SchedulingActivityExecutionId;
        }

        // Reverse to get root-to-leaf order
        chain.Reverse();

        var totalCount = chain.Count;

        // Apply pagination if specified
        if (skip.HasValue)
            chain = chain.Skip(skip.Value).ToList();
        if (take.HasValue)
            chain = chain.Take(take.Value).ToList();

        return Task.FromResult(Page.Of(chain, totalCount));
    }

    private static IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}
