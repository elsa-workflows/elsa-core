using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowDefinitionStore : YesSqlStore<WorkflowDefinition, WorkflowDefinitionDocument, WorkflowDefinitionIndex>, IWorkflowDefinitionStore
    {
        public YesSqlWorkflowDefinitionStore(ISession session, IIdGenerator idGenerator, IMapper mapper) : base(session, idGenerator, mapper, CollectionNames.WorkflowDefinitions)
        {
        }

        protected override async Task<WorkflowDefinitionDocument?> FindDocumentAsync(WorkflowDefinition entity, CancellationToken cancellationToken) => 
            await Query(x => x.EntityId == entity.EntityId).FirstOrDefaultAsync();
    }
}