using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IBuildsAndResumesWorkflow
    {
        Task<WorkflowInstance> BuildAndResumeWorkflowAsync<T>(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow;

        Task<WorkflowInstance> BuildAndResumeWorkflowAsync(
            IWorkflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);
    }
}