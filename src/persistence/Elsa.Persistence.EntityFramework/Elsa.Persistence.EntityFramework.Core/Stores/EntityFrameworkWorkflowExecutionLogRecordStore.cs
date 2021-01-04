using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : EntityFrameworkStore<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordEntity>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(ElsaContext dbContext) : base(dbContext)
        {
        }
    }
}