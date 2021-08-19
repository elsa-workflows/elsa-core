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
        public YesSqlWorkflowDefinitionStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowDefinitionStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WorkflowDefinitions)
        {
        }

        protected override async Task<WorkflowDefinitionDocument?> FindDocumentAsync(ISession session, WorkflowDefinition entity, CancellationToken cancellationToken) => await Query<WorkflowDefinitionIndex>(session, x => x.DefinitionVersionId == entity.VersionId).FirstOrDefaultAsync();

        protected override IQuery<WorkflowDefinitionDocument> MapSpecification(ISession session, ISpecification<WorkflowDefinition> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WorkflowDefinition> s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.Id),
                LatestOrPublishedWorkflowDefinitionIdSpecification s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.WorkflowDefinitionId),
                WorkflowDefinitionIdSpecification s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId == s.Id),
                
                ManyWorkflowDefinitionIdsSpecification s => s.VersionOptions == null 
                    ? Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId.IsIn(s.Ids)) 
                    : Query<WorkflowDefinitionIndex>(session, x => x.DefinitionId.IsIn(s.Ids)).WithVersion(s.VersionOptions),
                
                WorkflowDefinitionVersionIdSpecification s => Query<WorkflowDefinitionIndex>(session, x => x.DefinitionVersionId == s.VersionId),
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
    }
}