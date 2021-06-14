using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services.Workflows
{
    public class WorkflowExecutionLog : IWorkflowExecutionLog
    {
        private readonly IWorkflowExecutionLogStore _store;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public WorkflowExecutionLog(IWorkflowExecutionLogStore store, IIdGenerator idGenerator, IClock clock)
        {
            _store = store;
            _idGenerator = idGenerator;
            _clock = clock;
        }
        
        public async Task AddEntryAsync(string workflowInstanceId, string activityId, string activityType, string eventName, string? message, string? tenantId, string? source, JObject? data, CancellationToken cancellationToken = default)
        {
            var id = _idGenerator.Generate();
            var timeStamp = _clock.GetCurrentInstant();
            var record = new WorkflowExecutionLogRecord(id, tenantId, workflowInstanceId, activityId, activityType, timeStamp, eventName, message, source, data);
            await _store.SaveAsync(record, cancellationToken);
        }

        public Task<WorkflowExecutionLogRecord?> FindEntryAsync(ISpecification<WorkflowExecutionLogRecord> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);
    }
}