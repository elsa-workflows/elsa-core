using System;
using System.Linq;
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
    public class ActivityExecutionResultExecutedHandler : INotificationHandler<ActivityExecutionResultExecuted>, INotificationHandler<ActivityExecutionResultFailed>
    {
        private readonly IWorkflowTestService _workflowTestService;

        public ActivityExecutionResultExecutedHandler(IWorkflowTestService workflowTestService)
        {
            _workflowTestService = workflowTestService;
        }

        public async Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            var context = notification.ActivityExecutionContext;
            string? path = default;

            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowBlueprint.SignalRConnectionId;
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var data = new JObject
            {
                ["Outcomes"] = JToken.FromObject(context.Outcomes)
            };

            var body = context.Input != null? ((dynamic)context.Input).Body : null;
            
            if (body != null)
                data["Body"] = JToken.FromObject(body);

            var activityData = context.WorkflowInstance.ActivityData.FirstOrDefault();
            var pathProperty = activityData.Value.FirstOrDefault(x => x.Key == "Path");
            if (pathProperty.Value != null)
            {
                path = pathProperty.Value.ToString();
            }

            var message = new WorkflowTestMessage
            {
                Path = path,
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,
                Status = context.WorkflowExecutionContext.Status == WorkflowStatus.Running
                    ? "Executed"
                    : context.WorkflowExecutionContext.Status.ToString(),
                Data = JsonConvert.SerializeObject(data, Formatting.Indented)
            };

            await _workflowTestService.DispatchMessage(signalRConnectionId, message);
        }

        public async Task Handle(ActivityExecutionResultFailed notification, CancellationToken cancellationToken)
        {
            var context = notification.ActivityExecutionContext;
            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowBlueprint.SignalRConnectionId;
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var message = new WorkflowTestMessage
            {
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,
                Status = "Failed",
                Error = notification.Exception.InnerException?.InnerException?.ToString()
            };

            await _workflowTestService.DispatchMessage(signalRConnectionId, message);
        }
    }
}