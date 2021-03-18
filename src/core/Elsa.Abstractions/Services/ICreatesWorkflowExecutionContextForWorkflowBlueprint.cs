using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// An object which can create a workflow execution context for a specified workflow blueprint.
    /// </summary>
    public interface ICreatesWorkflowExecutionContextForWorkflowBlueprint
    {
        /// <summary>
        /// Creates a workflow execution context for the specified workflow blueprint.
        /// </summary>
        /// <param name="workflowBlueprint">A workflow blueprint</param>
        /// <param name="cancellationToken">An optional cancellation token</param>
        /// <returns>A task for a workflow execution context</returns>
        Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(IWorkflowBlueprint workflowBlueprint,
                                                                           CancellationToken cancellationToken = default);
    }
}