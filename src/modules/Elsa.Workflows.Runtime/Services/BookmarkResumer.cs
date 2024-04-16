using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class BookmarkResumer(IWorkflowRuntime workflowRuntime, IBookmarkStore bookmarkStore, IStimulusHasher stimulusHasher) : IBookmarkResumer
{
    /// <inheritdoc />
    public Task<RunWorkflowInstanceResponse> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return ResumeAsync<TActivity>(stimulus, null, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<TActivity>();
        var stimulusHash = stimulusHasher.Hash(activityTypeName, stimulus);
        var bookmarkFilter = new BookmarkFilter
        {
            ActivityTypeName = activityTypeName,
            WorkflowInstanceId = workflowInstanceId,
            Hash = stimulusHash,
        };
        return await ResumeAsync(bookmarkFilter, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmark = await bookmarkStore.FindAsync(filter, cancellationToken);

        if (bookmark == null)
            throw new Exception($"No bookmark matching the specified filter was found.");

        var workflowClient = await workflowRuntime.CreateClientAsync(bookmark.WorkflowInstanceId, cancellationToken);
        var runRequest = new RunWorkflowInstanceRequest
        {
            Input = options?.Input,
            Properties = options?.Properties,
            BookmarkId = bookmark.BookmarkId
        };
        return await workflowClient.RunAsync(runRequest, cancellationToken);
    }
}