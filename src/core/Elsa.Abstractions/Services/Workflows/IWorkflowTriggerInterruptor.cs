using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Causes a suspended workflow's trigger to be interrupted 
    /// </summary>
    public interface IWorkflowTriggerInterruptor
    {
        Task<RunWorkflowResult> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        Task<RunWorkflowResult> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityType, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(WorkflowInstance workflowInstance, string activityType, WorkflowInput? input = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<RunWorkflowResult>> InterruptActivityTypeAsync(string activityType, WorkflowInput? input = default, CancellationToken cancellationToken = default);
    }
}