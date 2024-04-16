using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Resumes a workflow.
/// </summary>
public interface IBookmarkResumer
{
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<RunWorkflowInstanceResponse> ResumeAsync<TActivity>(object stimulus, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity;
    
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<RunWorkflowInstanceResponse> ResumeAsync<TActivity>(object stimulus, string? workflowInstanceId, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default) where TActivity : IActivity;
    
    /// <summary>
    /// Resumes the bookmark.
    /// </summary>
    Task<RunWorkflowInstanceResponse> ResumeAsync(BookmarkFilter filter, ResumeBookmarkOptions? options, CancellationToken cancellationToken = default);
}