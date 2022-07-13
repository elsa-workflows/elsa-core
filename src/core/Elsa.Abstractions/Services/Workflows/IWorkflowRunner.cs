using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRunner
    {
        Task<RunWorkflowResult> RunWorkflowAsync(
            IWorkflowBlueprint workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            CancellationToken cancellationToken = default);
    }
}