using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.WorkflowTesting.Services
{
    public interface IWorkflowTestLaunchpad
    {
        /// <summary>
        /// Creates and executes workflow for the specified workflow blueprint, using the specified starting activity ID, with activity data for all activities prior to starting one copied from previous workflow instance.
        /// </summary>
        Task<RunWorkflowResult?> FindAndRestartTestWorkflowAsync(string workflowDefinitionId, string activityId, int version, string signalRConnectionId, string lastWorkflowInstanceId, string? tenantId = default, CancellationToken cancellationToken = default);
    }
}