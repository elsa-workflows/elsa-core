using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlBookmarkStore : YesSqlStore<Bookmark, BookmarkDocument>, IBookmarkStore
    {
        public YesSqlBookmarkStore(ISession session, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlBookmarkStore> logger) : base(session, idGenerator, mapper, logger, CollectionNames.Bookmarks)
        {
        }

        protected override async Task<BookmarkDocument?> FindDocumentAsync(Bookmark entity, CancellationToken cancellationToken) => await Query<BookmarkIndex>(x => x.BookmarkId == entity.Id).FirstOrDefaultAsync();
        protected override IQuery<BookmarkDocument> MapSpecification(ISpecification<Bookmark> specification) => AutoMapSpecification<BookmarkIndex>(specification);
    }
}