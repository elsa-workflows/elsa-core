using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using YesSql;
using YesSql.Services;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlBookmarkStore : YesSqlStore<Bookmark, BookmarkDocument>, IBookmarkStore
    {
        public YesSqlBookmarkStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlBookmarkStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.Bookmarks)
        {
        }

        protected override async Task<BookmarkDocument?> FindDocumentAsync(ISession session, Bookmark entity, CancellationToken cancellationToken) => await Query<BookmarkIndex>(session, x => x.BookmarkId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<BookmarkDocument> MapSpecification(ISession session, ISpecification<Bookmark> specification) =>
            specification switch
            {
                EntityIdSpecification<Bookmark> spec => Query<BookmarkIndex>(session, x => x.BookmarkId == spec.Id),
                BookmarkIdsSpecification spec => Query<BookmarkIndex>(session, x => x.BookmarkId.IsIn(spec.Ids)),
                WorkflowInstanceIdSpecification spec => Query<BookmarkIndex>(session, x => x.WorkflowInstanceId == spec.WorkflowInstanceId),
                WorkflowInstanceIdsSpecification spec => Query<BookmarkIndex>(session, x => x.WorkflowInstanceId.IsIn(spec.WorkflowInstanceIds)),
                _ => AutoMapSpecification<BookmarkIndex>(session, specification)
            };
    }
}