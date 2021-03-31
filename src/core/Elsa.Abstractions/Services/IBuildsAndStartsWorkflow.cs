using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBuildsAndStartsWorkflow
    {
        Task<WorkflowInstance> BuildAndStartWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow;

        public Task<WorkflowInstance> BuildAndStartWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}