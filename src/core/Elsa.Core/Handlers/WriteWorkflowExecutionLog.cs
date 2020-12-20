using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuted>
    {
        private readonly IWorkflowExecutionLogStore _store;
        private readonly IClock _clock;
        private readonly JsonSerializer _jsonSerializer;

        public WriteWorkflowExecutionLog(IWorkflowExecutionLogStore store, IClock clock, JsonSerializer jsonSerializer)
        {
            _store = store;
            _clock = clock;
            _jsonSerializer = jsonSerializer;
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
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
            var record = new WorkflowExecutionLogRecord(tenantId, workflowInstanceId, activityId, timeStamp, message, logData);

            await _store.SaveAsync(record, cancellationToken);
        }
    }
}