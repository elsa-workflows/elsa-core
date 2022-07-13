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

        public YesSqlWorkflowInstanceStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, IClock clock, ILogger<YesSqlWorkflowInstanceStore> logger) : base(sessionProvider, idGenerator, mapper, logger,
            CollectionNames.WorkflowInstances)
        {
            _clock = clock;
        }

        protected override async Task<WorkflowInstanceDocument?> FindDocumentAsync(ISession session, WorkflowInstance entity, CancellationToken cancellationToken) =>
            await Query<WorkflowInstanceIndex>(session, x => x.InstanceId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowInstanceDocument> MapSpecification(ISession session, ISpecification<WorkflowInstance> specification) =>
            specification switch
            {
                EntityIdSpecification<WorkflowInstance> spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId == spec.Id),
                WorkflowInstanceIdSpecification spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId == spec.Id),
                WorkflowInstanceIdsSpecification spec => Query<WorkflowInstanceIndex>(session, x => x.InstanceId.IsIn(spec.WorkflowInstanceIds)),
                AndSpecification<WorkflowInstance> spec => MapAndSpecification(session, spec),
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

        // TODO: This is a workaround. The real fix might be to remove the specification pattern and replace with repository abstractions with finite methods, or automatically dig into And and Or specifications and map each branch into a YesSQL queryable predicate.
        private IQuery<WorkflowInstanceDocument> MapAndSpecification(ISession session, AndSpecification<WorkflowInstance> and)
        {
            var left = and.Left;
            var right = and.Right;

            if (left is WorkflowCreatedBeforeSpecification workflowCreatedBeforeSpecification && right is WorkflowFinishedStatusSpecification workflowFinishedStatusSpecification)
            {
                var createdAtFilter = workflowCreatedBeforeSpecification.Instant.ToDateTimeUtc();
                var rightExpression = AutoMapSpecification<WorkflowInstanceIndex>(workflowFinishedStatusSpecification);
                return Query<WorkflowInstanceIndex>(session, x => x.CreatedAt <= createdAtFilter).Where(rightExpression);
            }

            if (left is UnfinishedWorkflowSpecification unfinishedWorkflowSpecification && right is WorkflowDefinitionVersionIdsSpecification workflowDefinitionVersionIdsSpecification)
            {
                var ids = workflowDefinitionVersionIdsSpecification.WorkflowDefinitionVersionIds;
                var leftExpression = AutoMapSpecification<WorkflowInstanceIndex>(unfinishedWorkflowSpecification);
                return Query<WorkflowInstanceIndex>(session, x => x.DefinitionVersionId.IsIn(ids)).Where(leftExpression);
            }

            return AutoMapSpecification<WorkflowInstanceIndex>(session, and);
        }
    }
}