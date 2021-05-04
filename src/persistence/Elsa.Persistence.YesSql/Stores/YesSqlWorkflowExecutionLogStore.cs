using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Data;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Services;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowExecutionLogStore : YesSqlStore<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordDocument>, IWorkflowExecutionLogStore
    {
        public YesSqlWorkflowExecutionLogStore(ISessionProvider sessionProvider, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowExecutionLogStore> logger) : base(sessionProvider, idGenerator, mapper, logger, CollectionNames.WorkflowExecutionLog)
        {
        }

        protected override async Task<WorkflowExecutionLogRecordDocument?> FindDocumentAsync(ISession session, WorkflowExecutionLogRecord entity, CancellationToken cancellationToken) => await Query<WorkflowExecutionLogRecordIndex>(session, x => x.RecordId == entity.Id).FirstOrDefaultAsync();
        protected override IQuery<WorkflowExecutionLogRecordDocument> MapSpecification(ISession session, ISpecification<WorkflowExecutionLogRecord> specification) => AutoMapSpecification<WorkflowExecutionLogRecordIndex>(session, specification);
    }
}