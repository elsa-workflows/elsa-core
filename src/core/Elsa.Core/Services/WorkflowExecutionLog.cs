﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Services
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
        
        public async Task AddEntryAsync(string message, string workflowInstanceId, string activityId, string activityType, string? tenantId, string? source, JObject? data, CancellationToken cancellationToken)
        {
            var id = _idGenerator.Generate();
            var timeStamp = _clock.GetCurrentInstant();
            var record = new WorkflowExecutionLogRecord(id, tenantId, workflowInstanceId, activityId, activityType, timeStamp, message, source, data);
            await _store.SaveAsync(record, cancellationToken);
        }
    }
}