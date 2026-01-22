using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Stores;

public class NoopActivityExecutionStore : IActivityExecutionStore
{
    public Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task AddManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ActivityExecutionRecord?>(null);
    }

    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ActivityExecutionRecord>>(Array.Empty<ActivityExecutionRecord>());
    }

    public Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ActivityExecutionRecord>>(Array.Empty<ActivityExecutionRecord>());
    }

    public Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ActivityExecutionRecordSummary>>(Array.Empty<ActivityExecutionRecordSummary>());
    }

    public Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ActivityExecutionRecordSummary>>(Array.Empty<ActivityExecutionRecordSummary>());
    }

    public Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }

    public Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0L);
    }

    public Task<Results.PagedCallStackResult> GetExecutionChainAsync(
        string activityExecutionId,
        bool includeCrossWorkflowChain = true,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Results.PagedCallStackResult
        {
            Items = Array.Empty<ActivityExecutionRecord>(),
            TotalCount = 0,
            Skip = skip,
            Take = take
        });
    }
}