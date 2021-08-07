using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IStartsWorkflow
    {
        Task<RunWorkflowResult> StartWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default);
    }
}