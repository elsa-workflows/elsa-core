using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using YesSql;
using YesSql.Services;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowDefinitionStore : YesSqlStore<WorkflowDefinition, WorkflowDefinitionDocument>, IWorkflowDefinitionStore
    {
        public YesSqlWorkflowDefinitionStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowDefinitionStore> logger) : base(sessionProvider, idGenerator, mapper, logger,
            CollectionNames.WorkflowDefinitions)
        {
        }

        protected override async Task<WorkflowDefinitionDocument?> FindDocumentAsync(ISession session, WorkflowDefinition entity, CancellationToken cancellationToken) =>
            await Query<WorkflowDefinitionIndex>(session, x => x.DefinitionVersionId == entity.VersionId).FirstOrDefaultAsync();

        protected override IQuery<WorkflowDefinitionDocument> MapSpecification(ISession session, ISpecification<WorkflowDefinition> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WorkflowDefinition> s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.Id),
                LatestOrPublishedWorkflowDefinitionIdSpecification s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.WorkflowDefinitionId && (x.IsLatest || x.IsPublished)),
                WorkflowDefinitionIdSpecification s => s.VersionOptions == null
                    ? Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.Id)
                    : Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.Id).WithVersion(s.VersionOptions),

                ManyWorkflowDefinitionIdsSpecification s => s.VersionOptions == null
                    ? Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId.IsIn(s.Ids))
                    : Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId.IsIn(s.Ids)).WithVersion(s.VersionOptions),

                WorkflowDefinitionVersionIdSpecification s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionVersionId == s.VersionId),
                AndSpecification<WorkflowDefinition> s => MapAndSpecification(session, s),
                VersionOptionsSpecification s => Query<WorkflowDefinitionIndex>(session).WithVersion(s.VersionOptions),
                _ => AutoMapSpecification<WorkflowDefinitionIndex>(session, specification)
            };
        }

        protected override IQuery<WorkflowDefinitionDocument> OrderBy(IQuery<WorkflowDefinitionDocument> query, IOrderBy<WorkflowDefinition> orderBy, ISpecification<WorkflowDefinition> specification)
        {
            var expression = orderBy.OrderByExpression.ConvertType<WorkflowDefinition, WorkflowDefinitionDocument>().ConvertType<WorkflowDefinitionDocument, WorkflowDefinitionIndex>();
            var indexedQuery = query.With<WorkflowDefinitionIndex>();
            return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
        }
        
        // TODO: This is a workaround. The real fix might be to remove the specification pattern and replace with repository abstractions with finite methods, or automatically dig into And and Or specifications and map each branch into a YesSQL queryable predicate.
        private IQuery<WorkflowDefinitionDocument> MapAndSpecification(ISession session, AndSpecification<WorkflowDefinition> and)
        {
            var left = and.Left;
            var right = and.Right;
            
            if(left is ManyWorkflowDefinitionIdsSpecification manyWorkflowDefinitionIdsSpecification && right is TenantSpecification<WorkflowDefinition> tenantSpecification)
            {
                return CreateQuery(session, manyWorkflowDefinitionIdsSpecification.Ids, manyWorkflowDefinitionIdsSpecification.VersionOptions, tenantSpecification.TenantId);
            }

            return AutoMapSpecification<WorkflowDefinitionIndex>(session, and);
        }

        private IQuery<WorkflowDefinitionDocument> CreateQuery(ISession session, IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, string? tenantId = default)
        {
            var query = Query<WorkflowDefinitionIndex>(session).Where(x => x.DefinitionId.IsIn(definitionIds));

            if (versionOptions != null)
                query.WithVersion(versionOptions);

            if (!string.IsNullOrWhiteSpace(tenantId))
                query.Where(x => x.TenantId == tenantId);

            return query;
        }
    }
}