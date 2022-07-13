using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using YesSql;
using YesSql.Services;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores;

public class YesSqlTriggerStore : YesSqlStore<Trigger, TriggerDocument>, ITriggerStore
{
    public YesSqlTriggerStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlTriggerStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.Triggers)
    {
    }

    protected override async Task<TriggerDocument?> FindDocumentAsync(ISession session, Trigger entity, CancellationToken cancellationToken) => await Query<TriggerIndex>(session, x => x.TriggerId == entity.Id).FirstOrDefaultAsync();

    protected override IQuery<TriggerDocument> MapSpecification(ISession session, ISpecification<Trigger> specification) =>
        specification switch
        {
            EntityIdSpecification<Trigger> spec => Query<TriggerIndex>(session, x => x.TriggerId == spec.Id),
            TriggerIdsSpecification spec => Query<TriggerIndex>(session, x => x.TriggerId.IsIn(spec.Ids)),
            WorkflowDefinitionIdSpecification spec => Query<TriggerIndex>(session, x => x.WorkflowDefinitionId == spec.WorkflowDefinitionId),
            WorkflowDefinitionIdsSpecification spec => Query<TriggerIndex>(session, x => x.WorkflowDefinitionId.IsIn(spec.WorkflowDefinitionIds)),
            TriggerSpecification spec => Query<TriggerIndex>(session, x => x.TenantId == spec.TenantId && x.ActivityType == spec.ActivityType && x.Hash.IsIn(spec.Hashes)),
            TriggerModelTypeSpecification spec => CreateTriggerModelTypeQuery(session, spec),
            _ => AutoMapSpecification<TriggerIndex>(session, specification)
        };

    private IQuery<TriggerDocument> CreateTriggerModelTypeQuery(ISession session, TriggerModelTypeSpecification spec)
    {
        var query = Query<TriggerIndex>(session, x => x.ModelType == spec.ModelType);

        if (!string.IsNullOrWhiteSpace(spec.TenantId))
            query = query.Where(x => x.TenantId == spec.TenantId);

        return query;
    }
}