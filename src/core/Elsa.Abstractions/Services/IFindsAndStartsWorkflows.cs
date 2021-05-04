using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Services
{
    public interface IFindsAndStartsWorkflows
    {
        Task FindAndStartWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}