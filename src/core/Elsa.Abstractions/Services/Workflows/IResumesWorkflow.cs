using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IResumesWorkflow
    {
        Task<RunWorkflowResult> ResumeWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            WorkflowInput? input = default,
            CancellationToken cancellationToken = default);
    }
}