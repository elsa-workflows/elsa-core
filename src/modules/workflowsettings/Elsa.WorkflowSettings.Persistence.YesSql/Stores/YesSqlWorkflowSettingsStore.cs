using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.Stores;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.YesSql.Documents;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Indexes;
using AutoMapper;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowSettingsStore : YesSqlStore<WorkflowSetting, WorkflowSettingsDocument>, IWorkflowSettingsStore
    {
        public YesSqlWorkflowSettingsStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowSettingsStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WorkflowSettings)
        {
        }

        protected override async Task<WorkflowSettingsDocument?> FindDocumentAsync(ISession session, WorkflowSetting entity, CancellationToken cancellationToken) => await Query<WorkflowSettingsIndex>(session, x => x.SettingId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowSettingsDocument> MapSpecification(ISession session, ISpecification<WorkflowSetting> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WorkflowSetting> s => Query<WorkflowSettingsIndex>(session, x => x.SettingId == s.Id),
                _ => AutoMapSpecification<WorkflowSettingsIndex>(session, specification)
            };
        }

        protected override IQuery<WorkflowSettingsDocument> OrderBy(IQuery<WorkflowSettingsDocument> query, IOrderBy<WorkflowSetting> orderBy, ISpecification<WorkflowSetting> specification)
        {
            var expression = orderBy.OrderByExpression.ConvertType<WorkflowSetting, WorkflowSettingsDocument>().ConvertType<WorkflowSettingsDocument, WorkflowSettingsIndex>();
            var indexedQuery = query.With<WorkflowSettingsIndex>();
            return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
        }
    }
}