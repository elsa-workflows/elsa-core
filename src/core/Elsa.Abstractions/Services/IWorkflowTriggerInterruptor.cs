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
        Task<WorkflowInstance> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityTypeAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityType, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityTypeAsync(WorkflowInstance workflowInstance, string activityType, object? input = default, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowInstance>> InterruptActivityTypeAsync(string activityType, object? input = default, CancellationToken cancellationToken = default);
    }
}