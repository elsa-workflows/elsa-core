using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuted>
    {
        private readonly IWorkflowExecutionLogStore _store;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;
        private readonly JsonSerializer _jsonSerializer;

        public WriteWorkflowExecutionLog(IWorkflowExecutionLogStore store, IIdGenerator idGenerator, IClock clock, JsonSerializer jsonSerializer)
        {
            _store = store;
            _idGenerator = idGenerator;
            _clock = clock;
            _jsonSerializer = jsonSerializer;
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            var id = _idGenerator.Generate();
            var tenantId = workflowInstance.TenantId;
            var workflowInstanceId = workflowInstance.Id;
            var activityId = notification.Activity.Id;
            var timeStamp = _clock.GetCurrentInstant();
            const string message = "Activity Executed";
            
            var state = new
            {
                notification.ActivityExecutionContext.Output,
                notification.ActivityExecutionContext.Data
            };

            var logData = JObject.FromObject(state, _jsonSerializer);
            var record = new WorkflowExecutionLogRecord(id, tenantId, workflowInstanceId, activityId, timeStamp, message, logData);

            await _store.SaveAsync(record, cancellationToken);
        }
    }
}