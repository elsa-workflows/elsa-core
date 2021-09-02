using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.Stores;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.YesSql.Documents;
using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using Elsa.WorkflowSettings.Persistence.YesSql.Indexes;
using AutoMapper;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowSettingsStore : YesSqlStore<WorkflowSetting, WorkflowSettingDocument>, IWorkflowSettingsStore
    {
        public YesSqlWorkflowSettingsStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowSettingsStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WorkflowSettings)
        {
        }

        protected override async Task<WorkflowSettingDocument?> FindDocumentAsync(ISession session, WorkflowSetting entity, CancellationToken cancellationToken) => await Query<WorkflowSettingIndex>(session, x => x.SettingId == entity.Id).FirstOrDefaultAsync();

        protected override IQuery<WorkflowSettingDocument> MapSpecification(ISession session, ISpecification<WorkflowSetting> specification)
        {
            return specification switch
            {
                EntityIdSpecification<WorkflowSetting> s => Query<WorkflowSettingIndex>(session, x => x.SettingId == s.Id),
                _ => AutoMapSpecification<WorkflowSettingIndex>(session, specification)
            };
        }

        protected override IQuery<WorkflowSettingDocument> OrderBy(IQuery<WorkflowSettingDocument> query, IOrderBy<WorkflowSetting> orderBy, ISpecification<WorkflowSetting> specification)
        {
            var expression = orderBy.OrderByExpression.ConvertType<WorkflowSetting, WorkflowSettingDocument>().ConvertType<WorkflowSettingDocument, WorkflowSettingIndex>();
            var indexedQuery = query.With<WorkflowSettingIndex>();
            return orderBy.SortDirection == SortDirection.Ascending ? indexedQuery.OrderBy(expression) : indexedQuery.OrderByDescending(expression);
        }
    }
}