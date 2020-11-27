using Elsa.Samples.ContextualWorkflowHttp.Models;
using YesSql.Indexes;

namespace Elsa.Samples.ContextualWorkflowHttp.Indexes
{
    public class DocumentIndex : MapIndex
    {
        public string DocumentUid { get; set; } = default!; // DocumentId is a reserved column name by YesSql, so taking DocumentUid instead.
    }

    public class DocumentIndexProvider : IndexProvider<Document>
    {
        public override void Describe(DescribeContext<Document> context)
        {
            context.For<DocumentIndex>().Map(
                x => new DocumentIndex
                {
                    DocumentUid = x.DocumentId
                });
        }
    }
}