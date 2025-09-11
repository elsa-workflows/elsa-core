using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Resumes workflows using a given stimulus or bookmark filter.
/// </summary>
public interface IWorkflowResumer
{
    /// <summary>
    /// Resumes the workflows associated with the bookmarks matching the given stimulus.
    /// </summary>
    Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// <summary>
    /// Resumes the workflow associated with the bookmark specified by the given bookmark ID.
    /// </summary>
    Task<RunWorkflowInstanceResponse?> ResumeAsync(string bookmarkId, IDictionary<string, object> input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the workflows associated with the bookmarks matching the given stimulus. If a workflow instance ID is specified, only resumes workflows associated with that instance.
    /// </summary>
    Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;
    
    /// <summary>
    /// Resumes the workflow associated with the bookmark specified by the given bookmark ID.
    /// </summary>
    Task<RunWorkflowInstanceResponse?> ResumeAsync<TActivity>(string bookmarkId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// Resumes the workflows associated with the bookmarks matching the given request.
    Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(ResumeBookmarkRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the workflows matching the given bookmark filter.
    /// </summary>
    Task<IEnumerable<RunWorkflowInstanceResponse>> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default);
}