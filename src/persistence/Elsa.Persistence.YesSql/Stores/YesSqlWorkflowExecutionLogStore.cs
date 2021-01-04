using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Data;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Persistence.YesSql.Indexes;
using Microsoft.Extensions.Logging;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Persistence.YesSql.Stores
{
    public class YesSqlWorkflowExecutionLogStore : YesSqlStore<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordDocument>, IWorkflowExecutionLogStore
    {
        public YesSqlWorkflowExecutionLogStore(ISession session, IIdGenerator idGenerator, IMapper mapper, ILogger<YesSqlWorkflowExecutionLogStore> logger) : base(session, idGenerator, mapper, logger, CollectionNames.WorkflowExecutionLog)
        {
        }

        protected override async Task<WorkflowExecutionLogRecordDocument?> FindDocumentAsync(WorkflowExecutionLogRecord entity, CancellationToken cancellationToken) => await Query<WorkflowExecutionLogRecordIndex>(x => x.RecordId == entity.Id).FirstOrDefaultAsync();
        protected override IQuery<WorkflowExecutionLogRecordDocument> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification) => AutoMapSpecification<WorkflowExecutionLogRecordIndex>(specification);
    }
}