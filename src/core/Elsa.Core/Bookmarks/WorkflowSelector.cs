using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowTriggers;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Bookmarks
{
    public class WorkflowSelector : IWorkflowSelector
    {
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IBookmarkHasher _hasher;
        private JsonSerializerSettings _serializerSettings;

        public WorkflowSelector(IBookmarkStore bookmarkStore, IBookmarkHasher hasher)
        {
            _bookmarkStore = bookmarkStore;
            _hasher = hasher;
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public async Task<IEnumerable<WorkflowSelectorResult>> SelectWorkflowsAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? tenantId, CancellationToken cancellationToken = default)
        {
            var triggerList = bookmarks as ICollection<IBookmark> ?? bookmarks.ToList();
            var specification = !triggerList.Any()
                ? new TriggerSpecification(activityType, tenantId)
                : BuildSpecification(activityType, triggerList, tenantId);

            var records = await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            return SelectResults(records);
        }

        private ISpecification<Bookmark> BuildSpecification(string activityType, IEnumerable<IBookmark> triggers, string? tenantId) => 
            triggers
                .Select(trigger => _hasher.Hash(trigger))
                .Aggregate(Specification<Bookmark>.None, (current, hash) => current.Or(new TriggerHashSpecification(hash, activityType, tenantId)));

        private IEnumerable<WorkflowSelectorResult> SelectResults(IEnumerable<Bookmark> workflowTriggers) =>
            from workflowTrigger in workflowTriggers
            let triggerType = Type.GetType(workflowTrigger.ModelType)
            let model = (IBookmark) JsonConvert.DeserializeObject(workflowTrigger.Model, triggerType, _serializerSettings)!
            select new WorkflowSelectorResult(workflowTrigger.WorkflowInstanceId, workflowTrigger.ActivityId, model);
    }
}