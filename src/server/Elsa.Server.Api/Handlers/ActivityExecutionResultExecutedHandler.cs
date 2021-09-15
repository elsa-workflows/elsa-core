using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.Api.Handlers
{
    public class ActivityExecutionResultExecutedHandler : INotificationHandler<ActivityExecutionResultExecuted>
    {
        private readonly IWorkflowTestService _workflowTestService;

        public ActivityExecutionResultExecutedHandler(IWorkflowTestService workflowTestService)
        {
            _workflowTestService = workflowTestService;
        }

        public async Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            var context = notification.ActivityExecutionContext;

            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowBlueprint.SignalRConnectionId;
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var data = new JObject
            {
                ["Outcomes"] = JToken.FromObject(context.Outcomes)
            };

            foreach (var entry in context.JournalData)
                data[entry.Key] = entry.Value != null ? JToken.FromObject(entry.Value) : JValue.CreateNull();

            var message = new WorkflowTestMessage
            {
                SignalRConnectionId = signalRConnectionId,
                WorkflowInstanceId = context.WorkflowInstance.Id,
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,
                Status = context.WorkflowExecutionContext.Status == WorkflowStatus.Running
                    ? "Executed"
                    : context.WorkflowExecutionContext.Status.ToString(),
                Data = JsonConvert.SerializeObject(data, Formatting.Indented)
            };

            await _workflowTestService.DispatchMessage(message);
        }
    }
}