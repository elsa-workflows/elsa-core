﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Causes a suspended workflow's trigger to be interrupted 
    /// </summary>
    public interface IWorkflowInterruptor
    {
        Task<WorkflowInstance> InterruptActivityAsync(WorkflowInstance workflowInstance, string activityId, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityId, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityTypeAsync(IWorkflowBlueprint workflowBlueprint, WorkflowInstance workflowInstance, string activityType, object? input = default, CancellationToken cancellationToken = default);
        Task<WorkflowInstance> InterruptActivityTypeAsync(WorkflowInstance workflowInstance, string activityType, object? input = default, CancellationToken cancellationToken = default);
    }
}