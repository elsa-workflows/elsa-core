using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IStartsWorkflow
    {
        Task<RunWorkflowResult> StartWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}