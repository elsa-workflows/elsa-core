using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Extensions;
using Elsa.Persistence.YesSql.Indexes;
using Microsoft.Extensions.Logging;
using YesSql;
using YesSql.Services;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowDefinitionStore : YesSqlStore<WorkflowDefinition, WorkflowDefinitionDocument>, IWorkflowDefinitionStore
    {
        public YesSqlWorkflowDefinitionStore(ISession session, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowDefinitionStore> logger) : base(session, idGenerator, mapper, logger, CollectionNames.WorkflowDefinitions)
        {
        }

        protected override async Task<WorkflowDefinitionDocument?> FindDocumentAsync(WorkflowDefinition entity, CancellationToken cancellationToken) => await Query<WorkflowDefinitionIndex>(x => x.DefinitionId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowDefinitionDocument> MapSpecification(ISpecification<WorkflowDefinition> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WorkflowDefinition> s => Query<WorkflowDefinitionIndex>(x => x.DefinitionId == s.Id),
                VersionOptionsSpecification s => Query<WorkflowDefinitionIndex>().WithVersion(s.VersionOptions),
                WorkflowDefinitionIdSpecification s => s.VersionOptions == null ? Query<WorkflowDefinitionIndex>(x => x.DefinitionId == s.Id) : Query<WorkflowDefinitionIndex>(x => x.DefinitionId == s.Id).WithVersion(s.VersionOptions),
                ManyWorkflowDefinitionVersionIdsSpecification s => Query<WorkflowDefinitionIndex>(x => x.DefinitionVersionId.IsIn(s.Ids)),
                _ => AutoMapSpecification<WorkflowDefinitionIndex>(specification)
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