using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Newtonsoft.Json;

namespace Elsa.Services.Triggers
{
    public class TriggerFinder : ITriggerFinder
    {
        private readonly ITriggerStore _triggerStore;
        private readonly JsonSerializerSettings _serializerSettings;

        public TriggerFinder(ITriggerStore triggerStore, JsonSerializerSettings serializerSettings)
        {
            _triggerStore = triggerStore;
            _serializerSettings = serializerSettings;
        }

        public async Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, CancellationToken cancellationToken = default)
        {
            var specification = new TriggerSpecification(activityType, tenantId);
            var records = await _triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            var filterList = filters as ICollection<IBookmark> ?? filters.ToList();
            var triggerResults = SelectResults(records);
            
            if (!filterList.Any())
                return triggerResults;

            var filteredTriggers = new List<TriggerFinderResult>();
            
            foreach (var triggerFinderResult in triggerResults)
            {
                foreach (var filter in filterList)
                {
                    var result  = triggerFinderResult.Bookmark.Compare(filter);

                    if (result == null || result.Value) 
                        filteredTriggers.Add(triggerFinderResult);
                }
            }

            return filteredTriggers;
        }

        public async Task<IEnumerable<TriggerFinderResult>> FindTriggersByTypeAsync(string modelType, string? tenantId, CancellationToken cancellationToken = default)
        {
            var specification = new TriggerModelTypeSpecification(modelType, tenantId);
            var triggers = await _triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return SelectResults(triggers);
        }

        private IEnumerable<TriggerFinderResult> SelectResults(IEnumerable<Trigger> triggers) =>
            from trigger in triggers
            let triggerType = Type.GetType(trigger.ModelType)
            let model = (IBookmark) JsonConvert.DeserializeObject(trigger.Model, triggerType, _serializerSettings)!
            select new TriggerFinderResult(trigger.WorkflowDefinitionId, trigger.ActivityId, model);
    }
}