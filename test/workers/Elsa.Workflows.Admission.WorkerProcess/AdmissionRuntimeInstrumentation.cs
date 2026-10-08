using Elsa.Common.Models;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elsa.Workflows.Admission.WorkerProcess;

// Only fixture services expose the context to deterministic mutation assertions. They never
// receive/mint a permit and always delegate to the real production extractor and commit.
public sealed class AdmissionObservedStateExtractor(IWorkflowStateExtractor inner, AdmissionRuntimeProbe probe) : IWorkflowStateExtractor
{
    public WorkflowState Extract(WorkflowExecutionContext context)
    {
        probe.PreparedContext = context;
        return inner.Extract(context);
    }
    public async Task<WorkflowExecutionContext> ApplyAsync(WorkflowExecutionContext context, WorkflowState state)
    {
        var restored = await inner.ApplyAsync(context, state);
        if (probe.OnRestored != null)
        {
            await probe.OnRestored(restored);
        }
        return restored;
    }
}

public sealed class AdmissionObservedCommit(ICommitStateHandler inner, AdmissionRuntimeProbe probe) : ICommitStateHandler
{
    public async Task CommitAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        await probe.ReachAsync("FinalCommit");
        await inner.CommitAsync(context, cancellationToken);
        await probe.ReachAsync("FinalCommitCompleted");
    }
    public async Task CommitAsync(WorkflowExecutionContext context, WorkflowState state, CancellationToken cancellationToken = default)
    {
        await probe.ReachAsync("FinalCommit");
        await inner.CommitAsync(context, state, cancellationToken);
        await probe.ReachAsync("FinalCommitCompleted");
    }
}

public sealed class AdmissionObservedInstanceWrites(AdmissionRuntimeProbe probe) : SaveChangesInterceptor
{
    private int _updates;
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var entries = eventData.Context!.ChangeTracker.Entries<WorkflowInstance>().ToArray();
        if (entries.Any(x => x.State == EntityState.Added))
        {
            await probe.IncrementAsync("instanceInsertAttempts");
            await probe.ReachAsync("BeforeInstanceInsert");
        }
        if (entries.Any(x => x.State == EntityState.Modified))
        {
            var update = Interlocked.Increment(ref _updates);
            await probe.IncrementAsync("instanceWriteAttempts");
            if (update == 2)
            {
                await probe.ReachAsync("TrailingWrite");
            }
        }
        return result;
    }
}

public sealed class AdmissionObservedBookmarkStore(IBookmarkStore inner, AdmissionRuntimeProbe probe) : IBookmarkStore
{
    public async ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
    {
        await probe.IncrementAsync("bookmarkSaveCalls");
        await inner.SaveAsync(record, cancellationToken);
    }
    public async ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
    {
        await probe.IncrementAsync("bookmarkBatchCalls");
        await inner.SaveManyAsync(records, cancellationToken);
    }
    public ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
    public ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, cancellationToken);
    public ValueTask<Page<StoredBookmark>> FindManyAsync(BookmarkFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) => inner.FindManyAsync(filter, pageArgs, cancellationToken);
    public ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default) => inner.DeleteAsync(filter, cancellationToken);
}
