using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;

namespace Elsa.Services.Triggers
{
    public class TriggerFinder : ITriggerFinder
    {
        private readonly ITriggerStore _triggerStore;
        private readonly IBookmarkHasher _bookmarkHasher;

        public TriggerFinder(ITriggerStore triggerStore, IBookmarkHasher bookmarkHasher)
        {
            _triggerStore = triggerStore;
            _bookmarkHasher = bookmarkHasher;
        }

        public async Task<IEnumerable<TriggerFinderResult>> FindTriggersAsync(string activityType, IEnumerable<IBookmark> filters, string? tenantId, CancellationToken cancellationToken = default)
        {
            var allTriggers = (await _triggerStore.GetAsync(cancellationToken)).ToList();
            var scopedTriggers = allTriggers.Where(x => x.ActivityType == activityType && x.WorkflowBlueprint.TenantId == tenantId);
            var filterList = filters as ICollection<IBookmark> ?? filters.ToList();

            if (!filterList.Any())
            {
                return scopedTriggers.Select(x => new TriggerFinderResult(x.WorkflowBlueprint, x.ActivityId, x.ActivityType, x.Bookmark)).ToList();
            }

            var hashes = filterList.ToDictionary(x => _bookmarkHasher.Hash(x), x => x);
            List<WorkflowTrigger> matches = new();

            foreach (var scoped in scopedTriggers)
            {
                if (!hashes.TryGetValue(scoped.BookmarkHash, out var bookmark))
                    continue;

                var result = scoped.Bookmark.Compare(bookmark);
                if (result == null || result.Value)
                    matches.Add(scoped);
            }

            return matches.Select(x => new TriggerFinderResult(x.WorkflowBlueprint, x.ActivityId, x.ActivityType, x.Bookmark)).ToList();
        }
    }
}