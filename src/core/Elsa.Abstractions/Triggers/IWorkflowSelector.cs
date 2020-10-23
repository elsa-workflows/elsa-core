using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public interface IWorkflowSelector
    {
        Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default);

        public Task<IEnumerable<IActivityBlueprint>> GetTriggersAsync(
            IWorkflowBlueprint workflowBlueprint,
            IEnumerable<IActivityBlueprint> blockingActivities,
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            CancellationToken cancellationToken = default);
    }
}