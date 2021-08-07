using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IFindsAndResumesWorkflows
    {
        Task FindAndResumeWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}