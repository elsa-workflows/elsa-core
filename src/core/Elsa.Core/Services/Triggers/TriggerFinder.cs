using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Serialization;
using Newtonsoft.Json;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Triggers
{
    public class TriggerFinder : ITriggerFinder
    {
        private const int BatchSize = 250;
        private readonly ITriggerStore _triggerStore;
        private readonly IContentSerializer _contentSerializer;
        private readonly IBookmarkHasher _bookmarkHasher;

        public TriggerFinder(ITriggerStore triggerStore, IContentSerializer contentSerializer, IBookmarkHasher bookmarkHasher)
        {
            _triggerStore = triggerStore;
            _contentSerializer = contentSerializer;
            _bookmarkHasher = bookmarkHasher;
        }

        public async Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var filterList = filters as ICollection<IBookmark> ?? filters.ToList();
            var hashes = filterList.Select(x => _bookmarkHasher.Hash(x)).ToList();
            var specification = new TriggerSpecification(activityType, hashes, tenantId);
            var paging = Paging.Create(skip, take);
            var orderBy = new OrderBy<Trigger>(x => x.Id, SortDirection.Ascending);
            var records = await _triggerStore.FindManyAsync(specification, orderBy, paging, cancellationToken);
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

        public async IAsyncEnumerable<TriggerFinderResult> StreamTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId = default, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var skip = 0;
            var filterList = filters.ToList();

            while (true)
            {
                var results = await FindTriggersAsync(activityType, filterList, tenantId, skip, BatchSize, cancellationToken).ToList();

                if (!results.Any())
                    yield break;

                foreach (var result in results)
                    yield return result;

                skip += results.Count;
            }
        }

        public async Task<IEnumerable<Trigger>> FindTriggersByTypeAsync(string modelType, string? tenantId, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var specification = new TriggerModelTypeSpecification(modelType, tenantId);
            var paging = Paging.Create(skip, take);
            var orderBy = new OrderBy<Trigger>(x => x.Id, SortDirection.Ascending);
            return await _triggerStore.FindManyAsync(specification, orderBy, paging, cancellationToken);
        }

        public IAsyncEnumerable<Trigger> StreamTriggersByTypeAsync(string modelType, string? tenantId, CancellationToken cancellationToken = default) =>
            StreamPaginatedListAsync(skip => FindTriggersByTypeAsync(modelType, tenantId, skip, BatchSize, cancellationToken), cancellationToken);

        private static async IAsyncEnumerable<T> StreamPaginatedListAsync<T>(Func<int, Task<IEnumerable<T>>> select, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var skip = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var results = await select(skip).ToList();

                if (!results.Any())
                    yield break;

                foreach (var result in results)
                    yield return result;

                skip += results.Count;
            }
        }

        private IEnumerable<TriggerFinderResult> SelectResults(IEnumerable<Trigger> triggers) =>
            from trigger in triggers
            let triggerType = Type.GetType(trigger.ModelType)
            let model = (IBookmark)JsonConvert.DeserializeObject(trigger.Model, triggerType, (JsonSerializerSettings)_contentSerializer.GetSettings())!
            select new TriggerFinderResult(trigger.WorkflowDefinitionId, trigger.ActivityId, model);
    }
}