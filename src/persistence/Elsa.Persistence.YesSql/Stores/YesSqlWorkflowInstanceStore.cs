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
    public class YesSqlWorkflowInstanceStore : YesSqlStore<WorkflowInstance, WorkflowInstanceDocument, WorkflowInstanceIndex>, IWorkflowInstanceStore
    {
        public YesSqlWorkflowInstanceStore(ISession session, IIdGenerator idGenerator, IMapper mapper) : base(session, idGenerator, mapper, CollectionNames.WorkflowInstances)
        {
        }

        protected override async Task<WorkflowInstanceDocument?> FindDocumentAsync(WorkflowInstance entity, CancellationToken cancellationToken) => 
            await Query(x => x.EntityId == entity.EntityId).FirstOrDefaultAsync();
    }
}