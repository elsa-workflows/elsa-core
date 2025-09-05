using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowResumer(
    IWorkflowRuntime workflowRuntime,
    IBookmarkStore bookmarkStore,
    IStimulusHasher stimulusHasher,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    ILogger<WorkflowResumer> logger) : IWorkflowResumer
{
    /// <inheritdoc />
    public Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return ResumeAsync<TActivity>(stimulus, null, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId = null, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<TActivity>();
        var stimulusHash = stimulusHasher.Hash(activityTypeName, stimulus);
        var bookmarkFilter = new BookmarkFilter
        {
            Name = activityTypeName,
            WorkflowInstanceId = workflowInstanceId,
            Hash = stimulusHash,
        };
        return await ResumeAsync(bookmarkFilter, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse?> ResumeAsync(string bookmarkId, IDictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var bookmarkFilter = new BookmarkFilter
        {
            BookmarkId = bookmarkId
        };
        var options = new ResumeBookmarkOptions
        {
            Input = input
        };
        var responses = await ResumeAsync(bookmarkFilter, options, cancellationToken);
        return responses.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse?> ResumeAsync<TActivity>(string bookmarkId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<TActivity>();
        var bookmarkFilter = new BookmarkFilter
        {
            Name = activityTypeName,
            BookmarkId = bookmarkId
        };
        var response = await ResumeAsync(bookmarkFilter, options, cancellationToken);
        return response.FirstOrDefault();
    }

    public async Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(ResumeBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        var filter = new BookmarkFilter
        {
            BookmarkId = request.BookmarkId,
            ActivityInstanceId = request.ActivityInstanceId ?? request.ActivityHandle?.ActivityInstanceId,
        };

        var resumeOptions = new ResumeBookmarkOptions()
        {
            Input = request.Input,
            Properties = request.Properties,
        };
        return await ResumeAsync(filter, resumeOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var hashableFilterString = filter.GetHashableString();
        var lockKey = $"bookmark-resumer:{hashableFilterString}";
        await using var filterLock = await distributedLockProvider.AcquireLockAsync(lockKey, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);
        var bookmarks = (await bookmarkStore.FindManyAsync(filter, cancellationToken)).ToList();

        if (bookmarks.Count == 0)
        {
            logger.LogDebug("No bookmarks found in store for filter {@Filter}", filter);
            return [];
        }

        var responses = new List<RunWorkflowInstanceResponse>();
        foreach (var bookmark in bookmarks)
        {
            var workflowClient = await workflowRuntime.CreateClientAsync(bookmark.WorkflowInstanceId, cancellationToken);
            var runRequest = new RunWorkflowInstanceRequest
            {
                Input = options?.Input,
                Properties = options?.Properties,
                BookmarkId = bookmark.Id
            };

            try
            {
                var response = await workflowClient.RunInstanceAsync(runRequest, cancellationToken);
                logger.LogDebug("Resumed workflow instance {WorkflowInstanceId} with bookmark {BookmarkId}", bookmark.WorkflowInstanceId, bookmark.Id);
                responses.Add(response);
            }
            catch (WorkflowInstanceNotFoundException)
            {
                // The workflow instance does not (yet) exist in the DB.
                logger.LogDebug("No workflow instance with ID {WorkflowInstanceId} found for bookmark {BookmarkId} at this time.", bookmark.WorkflowInstanceId, bookmark.Id);
            }
        }

        return responses;
    }
}