using Elsa.Http.Options;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Contracts;

/// <summary>
/// Updates the route table based on current workflow triggers and bookmarks.
/// </summary>
public interface IRouteTableUpdater
{
    /// <summary>
    /// Updates the route table based on current workflow triggers and bookmarks.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds routes to the route table based on the specified triggers.
    /// </summary>
    /// <param name="triggers">The triggers to create routes for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddRoutesAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds routes to the route table based on the specified bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to create routes for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddRoutesAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds routes to the route table based on the specified bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to create routes for.</param>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddRoutesAsync(IEnumerable<Bookmark> bookmarks, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes routes from the route table based on the specified triggers.
    /// </summary>
    /// <param name="triggers">The triggers to remove routes for.</param>
    void RemoveRoutes(IEnumerable<StoredTrigger> triggers);

    /// <summary>
    /// Removes routes from the route table based on the specified bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to remove routes for.</param>
    void RemoveRoutes(IEnumerable<Bookmark> bookmarks);
}