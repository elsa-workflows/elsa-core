using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IEnumerable<ITriggerProvider> _triggerProviders;

        public WorkflowSelector(IWorkflowRegistry workflowRegistry, IEnumerable<ITriggerProvider> triggerProviders)
        {
            _workflowRegistry = workflowRegistry;
            _triggerProviders = triggerProviders;
        }

        public async IAsyncEnumerable<WorkflowSelectorResult> SelectWorkflowsAsync(
            Type triggerType,
            Func<ITrigger, bool> evaluate,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var providers = _triggerProviders.Where(x => x.ForType() == triggerType).ToList();
            var workflowBlueprints = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToList();

            foreach (var provider in providers)
            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var triggers = provider.GetTriggersAsync(workflowBlueprint, cancellationToken);

                await foreach (var trigger in triggers.WithCancellation(cancellationToken))
                    if (evaluate(trigger))
                        yield return new WorkflowSelectorResult(workflowBlueprint, trigger.ActivityId);
            }
        }
    }
}