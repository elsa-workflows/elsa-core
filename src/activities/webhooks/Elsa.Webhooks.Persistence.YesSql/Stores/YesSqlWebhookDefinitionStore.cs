using System.Threading;
using System.Threading.Tasks;
using Elsa.Webhooks.Persistence.YesSql.Data;
using Elsa.Webhooks.Persistence.YesSql.Documents;
using Elsa.Webhooks.Persistence.YesSql.Indexes;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.Stores;
using AutoMapper;
using Elsa.Webhooks.Models;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Webhooks.Persistence.YesSql.Stores
{
    public class YesSqlWebhookDefinitionStore : YesSqlStore<WebhookDefinition, WebhookDefinitionDocument>, IWebhookDefinitionStore
    {
        public YesSqlWebhookDefinitionStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWebhookDefinitionStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WebhookDefinitions)
        {
        }

        protected override async Task<WebhookDefinitionDocument?> FindDocumentAsync(ISession session, WebhookDefinition entity, CancellationToken cancellationToken) => await Query<WebhookDefinitionIndex>(session, x => x.DefinitionId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WebhookDefinitionDocument> MapSpecification(ISession session, ISpecification<WebhookDefinition> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WebhookDefinition> s => Query<WebhookDefinitionIndex>(session, x => x.DefinitionId == s.Id),
                _ => AutoMapSpecification<WebhookDefinitionIndex>(session, specification)
            };
        }

        protected override IQuery<WebhookDefinitionDocument> OrderBy(IQuery<WebhookDefinitionDocument> query, IOrderBy<WebhookDefinition> orderBy, ISpecification<WebhookDefinition> specification)
        {
            var expression = orderBy.OrderByExpression.ConvertType<WebhookDefinition, WebhookDefinitionDocument>().ConvertType<WebhookDefinitionDocument, WebhookDefinitionIndex>();
            var indexedQuery = query.With<WebhookDefinitionIndex>();
            return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
        }
    }
}