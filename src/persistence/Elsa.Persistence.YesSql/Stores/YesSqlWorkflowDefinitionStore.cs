using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Microsoft.Extensions.Logging;
using YesSql;
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