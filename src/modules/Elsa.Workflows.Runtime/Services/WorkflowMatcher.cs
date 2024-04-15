using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowMatcher(IStimulusHasher stimulusHasher, ITriggerStore triggerStore, IBookmarkStore bookmarkStore) : IWorkflowMatcher
{
    /// <inheritdoc />
    public Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string activityTypeName, object stimulus, CancellationToken cancellationToken = default)
    {
        var hash = stimulusHasher.Hash(activityTypeName, stimulus);
        return FindTriggersAsync(activityTypeName, hash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(string activityTypeName, string stimulusHash, CancellationToken cancellationToken = default)
    {
        return await triggerStore.FindManyByStimulusHashAsync(stimulusHash, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var hash = stimulusHasher.Hash(activityTypeName, stimulus, options?.ActivityInstanceId);
        return FindBookmarksAsync(activityTypeName, hash, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(string activityTypeName, string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var correlationId = options?.CorrelationId;
        var workflowInstanceId = options?.WorkflowInstanceId;
        var activityInstanceId = options?.ActivityInstanceId;
        var filter = new BookmarkFilter
        {
            Hash = stimulusHash,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId
        };
        return await bookmarkStore.FindManyAsync(filter, cancellationToken);
    }
}