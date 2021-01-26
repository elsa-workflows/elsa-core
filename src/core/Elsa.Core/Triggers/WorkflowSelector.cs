using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowTriggers;
using Elsa.Serialization;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Triggers
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private readonly IWorkflowTriggerStore _workflowTriggerStore;
        private readonly IWorkflowTriggerHasher _hasher;
        private JsonSerializerSettings _serializerSettings;

        public WorkflowSelector(IWorkflowTriggerStore workflowTriggerStore, IWorkflowTriggerHasher hasher)
        {
            _workflowTriggerStore = workflowTriggerStore;
            _hasher = hasher;
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public async Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(string activityType, IEnumerable<ITrigger> triggers, string? tenantId, CancellationToken cancellationToken = default)
        {
            var triggerList = triggers as ICollection<ITrigger> ?? triggers.ToList();
            var specification = !triggerList.Any()
                ? new TriggerSpecification(activityType, tenantId)
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
            let triggerType = Type.GetType(workflowTrigger.TypeName)
            let model = (ITrigger) JsonConvert.DeserializeObject(workflowTrigger.Model, triggerType, _serializerSettings)!
            select new WorkflowSelectorResult(workflowTrigger.WorkflowInstanceId, workflowTrigger.ActivityId, model);
    }
}