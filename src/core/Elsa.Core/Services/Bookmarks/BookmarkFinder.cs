using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Services.Bookmarks
{
    public class BookmarkFinder : IBookmarkFinder
    {
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IBookmarkHasher _hasher;
        private readonly JsonSerializerSettings _serializerSettings;

        public BookmarkFinder(IBookmarkStore bookmarkStore, IBookmarkHasher hasher)
        {
            _bookmarkStore = bookmarkStore;
            _hasher = hasher;
            _serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public async Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId = default, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var triggerList = bookmarks as ICollection<IBookmark> ?? bookmarks.ToList();
            
            var specification = !triggerList.Any()
                ? new BookmarkSpecification(activityType, tenantId)
                : BuildSpecification(activityType, triggerList, correlationId, tenantId);

            var records = await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            return SelectResults(records);
        }

        private ISpecification<Bookmark> BuildSpecification(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId, string? tenantId)
        {
            var specification = bookmarks
                .Select(trigger => _hasher.Hash(trigger))
                .Aggregate(Specification<Bookmark>.None, (current, hash) => current.Or(new BookmarkHashSpecification(hash, activityType, tenantId)));

            if (correlationId != null)
                specification = specification.And(new CorrelationIdSpecification(correlationId));

            return specification;
        }

        private IEnumerable<BookmarkFinderResult> SelectResults(IEnumerable<Bookmark> bookmarks) =>
            from bookmark in bookmarks
            let bookmarkType = Type.GetType(bookmark.ModelType)
            let model = (IBookmark) JsonConvert.DeserializeObject(bookmark.Model, bookmarkType, _serializerSettings)!
            select new BookmarkFinderResult(bookmark.WorkflowInstanceId, bookmark.ActivityId, model, bookmark.CorrelationId);
    }
}