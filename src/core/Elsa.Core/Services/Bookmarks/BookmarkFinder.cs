using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Bookmarks
{
    public class BookmarkFinder : IBookmarkFinder
    {
        private const int BatchSize = 250;
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IBookmarkHasher _hasher;
        private readonly IBookmarkSerializer _serializer;

        public BookmarkFinder(IBookmarkStore bookmarkStore, IBookmarkHasher hasher, IBookmarkSerializer serializer)
        {
            _bookmarkStore = bookmarkStore;
            _hasher = hasher;
            _serializer = serializer;
        }

        public async Task<IEnumerable<BookmarkFinderResult>> FindBookmarksAsync(
            string activityType,
            IEnumerable<IBookmark> bookmarks,
            string? correlationId = default,
            string? tenantId = default,
            int skip = 0,
            int take = int.MaxValue,
            CancellationToken cancellationToken = default)
        {
            var bookmarkList = bookmarks as ICollection<IBookmark> ?? bookmarks.ToList();

            var specification = !bookmarkList.Any()
                ? new BookmarkSpecification(activityType, tenantId, correlationId)
                : BuildSpecification(activityType, bookmarkList, correlationId, tenantId);

            var paging = Paging.Create(skip, take);
            var orderBy = new OrderBy<Bookmark>(x => x.Id, SortDirection.Ascending);
            var records = await _bookmarkStore.FindManyAsync(specification, orderBy, paging, cancellationToken);
            return SelectResults(records);
        }

        public IAsyncEnumerable<BookmarkFinderResult> StreamBookmarksAsync(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId = default, string? tenantId = default, CancellationToken cancellationToken = default) =>
            StreamPaginatedListAsync(skip => FindBookmarksAsync(activityType, bookmarks, correlationId, tenantId, skip, BatchSize, cancellationToken), cancellationToken);
        
        public async Task<IEnumerable<Bookmark>> FindBookmarksByTypeAsync(string bookmarkType, string? tenantId = default, int skip = 0, int take = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var specification = new BookmarkTypeSpecification(bookmarkType, tenantId);
            var paging = Paging.Create(skip, take);
            var orderBy = new OrderBy<Bookmark>(x => x.Id, SortDirection.Ascending);
            return await _bookmarkStore.FindManyAsync(specification, orderBy, paging, cancellationToken);
        }

        public IAsyncEnumerable<Bookmark> StreamBookmarksByTypeAsync(string bookmarkType, string? tenantId = default, CancellationToken cancellationToken = default) =>
            StreamPaginatedListAsync(skip => FindBookmarksByTypeAsync(bookmarkType, tenantId, skip, BatchSize, cancellationToken), cancellationToken);

        private ISpecification<Bookmark> BuildSpecification(string activityType, IEnumerable<IBookmark> bookmarks, string? correlationId, string? tenantId)
        {
            var specification = bookmarks
                .Select(bookmark => _hasher.Hash(bookmark))
                .Aggregate(Specification<Bookmark>.None, (current, hash) => current.Or(new BookmarkHashSpecification(hash, activityType, tenantId)));

            if (correlationId != null)
                specification = specification.And(new CorrelationIdSpecification(correlationId));

            return specification;
        }

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

        private IEnumerable<BookmarkFinderResult> SelectResults(IEnumerable<Bookmark> bookmarks) =>
            from bookmark in bookmarks
            let bookmarkType = Type.GetType(bookmark.ModelType)
            let model = _serializer.Deserialize(bookmark.Model, bookmarkType)
            select new BookmarkFinderResult(bookmark.WorkflowInstanceId, bookmark.ActivityId, model, bookmark.CorrelationId);
    }
}