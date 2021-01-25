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
    public class YesSqlWorkflowTriggerStore : YesSqlStore<WorkflowTrigger, WorkflowTriggerDocument>, IWorkflowTriggerStore
    {
        public YesSqlWorkflowTriggerStore(ISession session, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowTriggerStore> logger) : base(session, idGenerator, mapper, logger, CollectionNames.WorkflowTriggers)
        {
        }

        protected override async Task<WorkflowTriggerDocument?> FindDocumentAsync(WorkflowTrigger entity, CancellationToken cancellationToken) => await Query<WorkflowTriggerIndex>(x => x.TriggerId == entity.Id).FirstOrDefaultAsync();
        protected override IQuery<WorkflowTriggerDocument> MapSpecification(ISpecification<WorkflowTrigger> specification) => AutoMapSpecification<WorkflowTriggerIndex>(specification);
    }
}