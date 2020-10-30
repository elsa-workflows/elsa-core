using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Timers.Triggers;
using Elsa.Indexes;
using Elsa.Services;
using Elsa.Triggers;
using NodaTime;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Timers.Services
{
    public class InstantEventManager : IInstantEventManager
    {
        private readonly IWorkflowSelector _workflowSelector;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IClock _clock;

        public InstantEventManager(IWorkflowSelector workflowSelector, IWorkflowInstanceManager workflowInstanceManager, IClock clock)
        {
            _workflowSelector = workflowSelector;
            _workflowInstanceManager = workflowInstanceManager;
            _clock = clock;
        }

        public async Task TriggerInstantEventsAsync(CancellationToken cancellationToken)
        {
            var now = _clock.GetCurrentInstant();
            var candidates = await _workflowSelector.SelectWorkflowsAsync<InstantEventTrigger>(x => x.Instant <= now, cancellationToken).ToList();

            foreach (var candidate in candidates)
            {
                // Only trigger workflows that haven't already executed.
                var instanceCount = await _workflowInstanceManager.Query<WorkflowInstanceIndex>(x => x.WorkflowDefinitionId == candidate.WorkflowBlueprint.Id).CountAsync();
                
                if(instanceCount > 0)
                    continue;
                
                
            }
        }
    }
}