using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IBuildsAndResumesWorkflow
    {
        Task<RunWorkflowResult> BuildAndResumeWorkflowAsync<T>(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            CancellationToken cancellationToken = default)
            where T : IWorkflow;

        Task<RunWorkflowResult> BuildAndResumeWorkflowAsync(
            IWorkflow workflow,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            CancellationToken cancellationToken = default);
    }
}