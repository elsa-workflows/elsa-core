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
    public class YesSqlSuspendedWorkflowStore : YesSqlStore<SuspendedWorkflowBlockingActivity, SuspendedWorkflowBlockingActivityDocument, SuspendedWorkflowIndex>, ISuspendedWorkflowStore
    {
        public YesSqlSuspendedWorkflowStore(ISession session, IIdGenerator idGenerator, IMapper mapper) : base(session, idGenerator, mapper, CollectionNames.SuspendedWorkflows)
        {
        }

        protected override async Task<SuspendedWorkflowBlockingActivityDocument?> FindDocumentAsync(SuspendedWorkflowBlockingActivity entity, CancellationToken cancellationToken) =>
            await Query(x => x.EntityId == entity.EntityId).FirstOrDefaultAsync();
    }
}