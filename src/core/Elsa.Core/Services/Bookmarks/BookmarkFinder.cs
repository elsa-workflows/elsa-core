using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;

namespace Elsa.Services.Bookmarks
{
    public class BookmarkFinder : IBookmarkFinder
    {
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IBookmarkHasher _hasher;
        private readonly IBookmarkSerializer _serializer;

        public BookmarkFinder(IBookmarkStore bookmarkStore, IBookmarkHasher hasher, IBookmarkSerializer serializer)
        {
            _bookmarkStore = bookmarkStore;
            _hasher = hasher;
            _serializer = serializer;
        }

        public async Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId = default, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var bookmarkList = bookmarks as ICollection<IBookmark> ?? bookmarks.ToList();
            
            var specification = !bookmarkList.Any()
                ? new BookmarkSpecification(activityType, tenantId, correlationId)
                : BuildSpecification(activityType, bookmarkList, correlationId, tenantId);

            var records = await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
            return SelectResults(records);
        }

        // TODO: Implement this to return all bookmarks of the specified bookmark model type.
        public async Task<IEnumerable<Bookmark>> FindBookmarksByTypeAsync(string bookmarkType, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var specification = new BookmarkTypeSpecification(bookmarkType, tenantId);
            return await _bookmarkStore.FindManyAsync(specification, cancellationToken: cancellationToken);
        }

        private ISpecification<Bookmark> BuildSpecification(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId, string? tenantId)
        {
            var specification = bookmarks
                .Select(bookmark => _hasher.Hash(bookmark))
                .Aggregate(Specification<Bookmark>.None, (current, hash) => current.Or(new BookmarkHashSpecification(hash, activityType, tenantId)));

            if (correlationId != null)
                specification = specification.And(new CorrelationIdSpecification(correlationId));

            return specification;
        }

        private IEnumerable<BookmarkFinderResult> SelectResults(IEnumerable<Bookmark> bookmarks) =>
            from bookmark in bookmarks
            let bookmarkType = Type.GetType(bookmark.ModelType)
            let model = _serializer.Deserialize(bookmark.Model, bookmarkType)
            select new BookmarkFinderResult(bookmark.WorkflowInstanceId, bookmark.ActivityId, model, bookmark.CorrelationId);
    }
}