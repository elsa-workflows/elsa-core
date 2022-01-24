using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Serialization;
using Newtonsoft.Json;

namespace Elsa.Services.Triggers
{
    public class TriggerFinder : ITriggerFinder
    {
        private readonly ITriggerStore _triggerStore;
        private readonly IContentSerializer _contentSerializer;
        private readonly IBookmarkHasher _bookmarkHasher;

        public TriggerFinder(ITriggerStore triggerStore, IContentSerializer contentSerializer, IBookmarkHasher bookmarkHasher)
        {
            _triggerStore = triggerStore;
            _contentSerializer = contentSerializer;
            _bookmarkHasher = bookmarkHasher;
        }

        public async Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, CancellationToken cancellationToken = default)
        {
            var filterList = filters as ICollection<IBookmark> ?? filters.ToList();
            var hashes = filterList.Select(x => _bookmarkHasher.Hash(x)).ToList();
            var specification = new TriggerSpecification(activityType, hashes, tenantId);
            var records = await _triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            var triggerResults = SelectResults(records);

            if (!filterList.Any())
                return triggerResults;

            var query =
                from triggerFinderResult in triggerResults
                from filter in filterList
                let result = triggerFinderResult.Bookmark.Compare(filter)
                where result == null || result.Value
                select triggerFinderResult;

            return query;
        }

        public async Task<IEnumerable<Trigger>> FindTriggersByTypeAsync(string modelType, string? tenantId, CancellationToken cancellationToken = default)
        {
            var specification = new TriggerModelTypeSpecification(modelType, tenantId);
            return await _triggerStore.FindManyAsync(specification, cancellationToken: cancellationToken);
        }

        private IEnumerable<TriggerFinderResult> SelectResults(IEnumerable<Trigger> triggers) =>
            from trigger in triggers
            let triggerType = Type.GetType(trigger.ModelType)
            let model = (IBookmark)JsonConvert.DeserializeObject(trigger.Model, triggerType, (JsonSerializerSettings)_contentSerializer.GetSettings())!
            select new TriggerFinderResult(trigger.WorkflowDefinitionId, trigger.ActivityId, model);
    }
}