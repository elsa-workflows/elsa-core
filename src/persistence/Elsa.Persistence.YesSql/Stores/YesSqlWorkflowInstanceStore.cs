using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using NodaTime;
using YesSql;
using YesSql.Services;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowInstanceStore : YesSqlStore<WorkflowInstance, WorkflowInstanceDocument>, IWorkflowInstanceStore
    {
        private readonly IClock _clock;

        public YesSqlWorkflowInstanceStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, IClock clock, ILogger<YesSqlWorkflowInstanceStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WorkflowInstances)
        {
            _clock = clock;
        }

        protected override async Task<WorkflowInstanceDocument?> FindDocumentAsync(ISession session, WorkflowInstance entity, CancellationToken cancellationToken) => await Query<WorkflowInstanceIndex>(session, x => x.InstanceId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowInstanceDocument> MapSpecification(ISession session, ISpecification<WorkflowInstance> specification) =>
            specification switch
            {
                EntityIdSpecification<WorkflowInstance> spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId == spec.Id),
                WorkflowInstanceIdSpecification spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId == spec.Id),
                WorkflowInstanceIdsSpecification spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId.IsIn(spec.WorkflowInstanceIds)),
                _ => AutoMapSpecification<WorkflowInstanceIndex>(session, specification)
            };

        protected override IQuery<WorkflowInstanceDocument> OrderBy(IQuery<WorkflowInstanceDocument> query, IOrderBy<WorkflowInstance> orderBy, ISpecification<WorkflowInstance> specification)
        {
            switch (specification)
            {
                default:
                {
                    var indexedQuery = query.With<WorkflowInstanceIndex>();
                    var expression = orderBy.OrderByExpression.ConvertType<WorkflowInstance, WorkflowInstanceDocument>().ConvertType<WorkflowInstanceDocument, WorkflowInstanceIndex>();
                    return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
                }
            }
        }
    }
}