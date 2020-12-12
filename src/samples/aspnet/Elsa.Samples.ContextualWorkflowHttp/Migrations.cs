using Elsa.Persistence.YesSql.Data;
using Elsa.Samples.ContextualWorkflowHttp.Indexes;
using YesSql.Sql;

namespace Elsa.Samples.ContextualWorkflowHttp
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<DocumentIndex>(table => table.Column<string>(nameof(DocumentIndex.DocumentUid)));
            return 1;
        }
    }
}