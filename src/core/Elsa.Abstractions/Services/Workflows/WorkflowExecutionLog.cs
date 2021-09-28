using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowExecutionLog
    {
        private readonly IWorkflowExecutionLogStore _store;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;
        private readonly ICollection<WorkflowExecutionLogRecord> _records = new List<WorkflowExecutionLogRecord>();

        public WorkflowExecutionLog(IWorkflowExecutionLogStore store, IIdGenerator idGenerator, IClock clock)
        {
            _store = store;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public void AddEntry(string workflowInstanceId, string activityId, string activityType, string eventName, string? message, string? tenantId, string? source, JObject? data)
        {
            var id = _idGenerator.Generate();
            var timeStamp = _clock.GetCurrentInstant();
            var record = new WorkflowExecutionLogRecord(id, tenantId, workflowInstanceId, activityId, activityType, timeStamp, eventName, message, source, data);
            _records.Add(record);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await _store.AddManyAsync(_records, cancellationToken);
            _records.Clear();
        }

        public Task<WorkflowExecutionLogRecord?> FindEntryAsync(ISpecification<WorkflowExecutionLogRecord> specification, CancellationToken cancellationToken = default) => _store.FindAsync(specification, cancellationToken);
    }
}