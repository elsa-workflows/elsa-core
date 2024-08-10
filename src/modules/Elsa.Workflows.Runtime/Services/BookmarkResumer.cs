using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class BookmarkResumer(IWorkflowRuntime workflowRuntime, IBookmarkStore bookmarkStore, IStimulusHasher stimulusHasher, ILogger<BookmarkResumer> logger) : IBookmarkResumer
{
    /// <inheritdoc />
    public Task<ResumeBookmarkResult> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return ResumeAsync<TActivity>(stimulus, null, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResumeBookmarkResult> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity
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
    public async Task<ResumeBookmarkResult> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmark = await bookmarkStore.FindAsync(filter, cancellationToken);

        if (bookmark == null)
        {
            logger.LogDebug("Bookmark not found in store for filter {@Filter}", filter);
            return ResumeBookmarkResult.NotFound();
        }

        var workflowClient = await workflowRuntime.CreateClientAsync(bookmark.WorkflowInstanceId, cancellationToken);
        var runRequest = new RunWorkflowInstanceRequest
        {
            Input = options?.Input,
            Properties = options?.Properties,
            BookmarkId = bookmark.Id
        };
        var response = await workflowClient.RunInstanceAsync(runRequest, cancellationToken);
        logger.LogDebug("Resumed workflow instance {WorkflowInstanceId} with bookmark {BookmarkId}", bookmark.WorkflowInstanceId, bookmark.Id);
        return ResumeBookmarkResult.Found(response);
    }
}