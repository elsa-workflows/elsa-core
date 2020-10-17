using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Triggers;

namespace Elsa.Services
{
    public interface IWorkflowHost
    {
        ValueTask<WorkflowInstance> RunWorkflowAsync(
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);
        
        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowDefinition,
            WorkflowInstance workflowInstance,
            string? activityId = default,
            object? input = default,
            CancellationToken cancellationToken = default);

        ValueTask<WorkflowInstance> RunWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default);
    }
}