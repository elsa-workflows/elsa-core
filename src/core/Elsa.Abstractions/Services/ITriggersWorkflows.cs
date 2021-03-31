using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Starts new workflows and resumes existing ones by looking up all bookmarks that match the specified activity type, bookmark and trigger specification.
    /// These workflows are executed immediately (as opposed as being dispatched).
    /// </summary>
    public interface ITriggersWorkflows
    {
        Task<IEnumerable<WorkflowInstance>> TriggerWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            IBookmark trigger,
            string? correlationId,
            object? input = default,
            string? contextId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default);
    }
}