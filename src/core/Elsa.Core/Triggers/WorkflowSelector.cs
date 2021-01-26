using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowTriggers;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private readonly IWorkflowTriggerStore _workflowTriggerStore;
        private readonly IWorkflowTriggerHasher _hasher;
        private readonly IContentSerializer _contentSerializer;

        public WorkflowSelector(
            IWorkflowTriggerStore workflowTriggerStore,
            IWorkflowTriggerHasher hasher,
            IContentSerializer contentSerializer)
        {
            _workflowTriggerStore = workflowTriggerStore;
            _hasher = hasher;
            _contentSerializer = contentSerializer;
        }

        public async Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(string activityType, IEnumerable<ITrigger> triggers, string? tenantId, CancellationToken cancellationToken = default)
        {
            var triggerList = triggers as ICollection<ITrigger> ?? triggers.ToList();
            var specification = !triggerList.Any()
                ? (ISpecification<WorkflowTrigger>) new TriggerSpecification(activityType, tenantId)
                : BuildSpecification(activityType, triggerList, tenantId);

            var records = await _workflowTriggerStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            return SelectResults(records);
        }

        private ISpecification<WorkflowTrigger> BuildSpecification(string activityType, IEnumerable<ITrigger> triggers, string? tenantId) => 
            triggers
                .Select(trigger => _hasher.Hash(trigger))
                .Aggregate(Specification<WorkflowTrigger>.None, (current, hash) => current.Or(new TriggerHashSpecification(hash, activityType, tenantId)));

        private IEnumerable<WorkflowSelectorResult> SelectResults(IEnumerable<WorkflowTrigger> workflowTriggers) =>
            from workflowTrigger in workflowTriggers
            let model = (ITrigger) _contentSerializer.Deserialize(workflowTrigger.Model)!
            select new WorkflowSelectorResult(workflowTrigger.WorkflowInstanceId, workflowTrigger.ActivityId, model);
    }
}