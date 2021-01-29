using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Bookmarks
{
    public interface IWorkflowSelector
    {
        Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? tenantId, CancellationToken cancellationToken = default);
    }
}