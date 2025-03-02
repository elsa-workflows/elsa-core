using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowMatcher(IStimulusHasher stimulusHasher, ITriggerStore triggerStore, IBookmarkStore bookmarkStore) : IWorkflowMatcher
{
    /// <inheritdoc />
    public Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string activityTypeName, object stimulus, CancellationToken cancellationToken = default)
    {
        var hash = stimulusHasher.Hash(activityTypeName, stimulus);
        return FindTriggersAsync(hash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string stimulusHash, CancellationToken cancellationToken = default)
    {
        return await triggerStore.FindManyByStimulusHashAsync(stimulusHash, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var hash = stimulusHasher.Hash(activityTypeName, stimulus, options?.ActivityInstanceId);
        return FindBookmarksAsync(hash, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.WorkflowInstanceId;
        var activityInstanceId = options?.ActivityInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = stimulusHash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            BookmarkId = options?.BookmarkId
        };
        return await bookmarkStore.FindManyAsync(filter, cancellationToken);
    }
}