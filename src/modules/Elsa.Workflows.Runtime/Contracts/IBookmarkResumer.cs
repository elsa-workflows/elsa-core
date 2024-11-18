using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Resumes workflows using a given stimulus or bookmark filter.
/// </summary>
public interface IBookmarkResumer
{
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<ResumeBookmarkResult> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<ResumeBookmarkResult> ResumeAsync(string bookmarkId, IDictionary<string, object> input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<ResumeBookmarkResult> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;
    
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<ResumeBookmarkResult> ResumeAsync<TActivity>(string bookmarkId, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// Resumes the bookmark.
    Task<ResumeBookmarkResult> ResumeAsync(ResumeBookmarkRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<ResumeBookmarkResult> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options = null, CancellationToken cancellationToken = default);
}