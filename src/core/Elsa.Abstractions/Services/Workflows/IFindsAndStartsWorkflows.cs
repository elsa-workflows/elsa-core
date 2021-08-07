using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IFindsAndStartsWorkflows
    {
        Task FindAndStartWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            WorkflowInput? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}