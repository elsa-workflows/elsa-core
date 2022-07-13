using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class BookmarkIndex : MapIndex
    {
        public string? TenantId { get; set; }
        public string BookmarkId { get; set; } = default!;
        public string Hash { get; set; } = default!;
        public string ModelType { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public string WorkflowInstanceId { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }
    
    public class BookmarkIndexProvider : IndexProvider<BookmarkDocument>
    {
        public BookmarkIndexProvider() => CollectionName = CollectionNames.Bookmarks;

        public override void Describe(DescribeContext<BookmarkDocument> context)
        {
            context.For<BookmarkIndex>()
                .Map(
                    record => new BookmarkIndex
                    {
                        TenantId = record.TenantId,
                        BookmarkId = record.BookmarkId,
                        Hash = record.Hash,
                        ModelType = record.ModelType,
                        ActivityType = record.ActivityType,
                        WorkflowInstanceId = record.WorkflowInstanceId,
                        CorrelationId = record.CorrelationId
                    }
                );
        }
    }
}