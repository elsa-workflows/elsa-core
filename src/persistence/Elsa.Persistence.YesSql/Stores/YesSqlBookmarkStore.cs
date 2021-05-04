using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlBookmarkStore : YesSqlStore<Bookmark, BookmarkDocument>, IBookmarkStore
    {
        public YesSqlBookmarkStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlBookmarkStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.Bookmarks)
        {
        }

        protected override async Task<BookmarkDocument?> FindDocumentAsync(ISession session, Bookmark entity, CancellationToken cancellationToken) => await Query<BookmarkIndex>(session, x => x.BookmarkId == entity.Id).FirstOrDefaultAsync();
        protected override IQuery<BookmarkDocument> MapSpecification(ISession session, ISpecification<Bookmark> specification) => AutoMapSpecification<BookmarkIndex>(session, specification);
    }
}