using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public interface IWorkflowSelector
    {
        Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(Type triggerType, Func<ITrigger, bool> evaluate, CancellationToken cancellationToken = default);

        public Task<IEnumerable<IActivityBlueprint>> GetTriggersAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default);

        Task UpdateTriggersAsync(IWorkflowBlueprint workflowBlueprint, string? workflowInstanceId, CancellationToken cancellationToken = default);
        Task RemoveTriggerAsync(ITrigger trigger, CancellationToken cancellationToken = default);
    }
}