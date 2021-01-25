using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Microsoft.Extensions.Logging;
using NodaTime;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowInstanceStore : YesSqlStore<WorkflowInstance, WorkflowInstanceDocument>, IWorkflowInstanceStore
    {
        private readonly IClock _clock;

        public YesSqlWorkflowInstanceStore(ISession session, IIdGenerator idGenerator, IMapper mapper, IClock clock, ILogger<YesSqlWorkflowInstanceStore> logger) : base(session, idGenerator, mapper, logger, CollectionNames.WorkflowInstances)
        {
            _clock = clock;
        }

        protected override async Task<WorkflowInstanceDocument?> FindDocumentAsync(WorkflowInstance entity, CancellationToken cancellationToken) => await Query<WorkflowInstanceIndex>(x => x.InstanceId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowInstanceDocument> MapSpecification(ISpecification<WorkflowInstance> specification) =>
            specification switch
            {
                EntityIdSpecification<WorkflowInstance> spec => Query<WorkflowInstanceIndex>(x => x.InstanceId == spec.Id),
                WorkflowInstanceIdSpecification spec => Query<WorkflowInstanceIndex>(x => x.InstanceId == spec.Id),
                BlockingActivityTypeSpecification spec => Query<WorkflowInstanceBlockingActivitiesIndex>(x => x.ActivityType == spec.ActivityType),
                _ => AutoMapSpecification<WorkflowInstanceIndex>(specification)
            };

        protected override IQuery<WorkflowInstanceDocument> OrderBy(IQuery<WorkflowInstanceDocument> query, IOrderBy<WorkflowInstance> orderBy, ISpecification<WorkflowInstance> specification)
        {
            switch (specification)
            {
                case BlockingActivityTypeSpecification:
                {
                    var indexedQuery = query.With<WorkflowInstanceBlockingActivitiesIndex>();
                    var expression = orderBy.OrderByExpression.ConvertType<WorkflowInstance, WorkflowInstanceDocument>().ConvertType<WorkflowInstanceDocument, WorkflowInstanceBlockingActivitiesIndex>();
                    return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
                }
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