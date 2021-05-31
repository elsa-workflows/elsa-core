using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IBuildsAndStartsWorkflow
    {
        Task<RunWorkflowResult> BuildAndStartWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow;

        public Task<RunWorkflowResult> BuildAndStartWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}