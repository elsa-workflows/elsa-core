using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using MediatR;
using NodaTime;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuted>
    {
        private readonly IWorkflowExecutionLogStore _store;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public WriteWorkflowExecutionLog(IWorkflowExecutionLogStore store, IIdGenerator idGenerator, IClock clock)
        {
            _store = store;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var workflowInstance = notification.WorkflowExecutionContext.WorkflowInstance;
            var id = _idGenerator.Generate();
            var tenantId = workflowInstance.TenantId;
            var workflowInstanceId = workflowInstance.Id;
            var activityId = notification.Activity.Id;
            var activityBlueprint = notification.Activity;
            var activityType = activityBlueprint.Type;
            var source = activityBlueprint.Source;
            var timeStamp = _clock.GetCurrentInstant();
            const string message = "Activity Executed";
            var record = new WorkflowExecutionLogRecord(id, tenantId, workflowInstanceId, activityId, activityType, timeStamp, message, source);
            await _store.SaveAsync(record, cancellationToken);
        }
    }
}