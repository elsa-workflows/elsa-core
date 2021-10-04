using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
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

            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowInstance.GetMetadata("signalRConnectionId")?.ToString();
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var data = new JObject
            {
                ["Outcomes"] = JToken.FromObject(context.Outcomes)
            };

            var body = context.Input != null ? ((dynamic)context.Input).Body : null;
            if (body != null)
                data["Body"] = JToken.FromObject(body);

            var activityData = context.WorkflowInstance.ActivityData.FirstOrDefault(x => x.Key == notification.ActivityExecutionContext.ActivityId).Value;

            var message = new WorkflowTestMessage
            {
                ActivityType = context.ActivityBlueprint.Type,
                WorkflowInstanceId = context.WorkflowInstance.Id,
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,
                WorkflowStatus = context.WorkflowExecutionContext.Status == WorkflowStatus.Running
                    ? "Executed"
                    : context.WorkflowExecutionContext.Status.ToString(),
                Data = JsonConvert.SerializeObject(data, Formatting.Indented),
                Status = GetExecutionResult(notification.Result),
                ActivityData = activityData
            };

            await _workflowTestService.DispatchMessage(signalRConnectionId, message);
        }

        private string GetExecutionResult(IActivityExecutionResult activityExecutionResult)
        {
            string status = string.Empty;

            switch (activityExecutionResult)
            {
                case SuspendResult:
                    status = "Waiting";
                    break;
                case DoneResult:
                case OutcomeResult:
                    var outcomeResult = (OutcomeResult)activityExecutionResult;
                    status = outcomeResult.Outcomes.FirstOrDefault();
                    break;
                case FaultResult:
                    status = "Failed";
                    break;
                case CombinedResult:
                    var combinedResult = (CombinedResult)activityExecutionResult;
                    status = GetExecutionResult(combinedResult.Results.FirstOrDefault());
                    break;
            }

            return status;
        }

        public async Task Handle(ActivityExecutionResultFailed notification, CancellationToken cancellationToken)
        {
            var context = notification.ActivityExecutionContext;
            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowInstance.GetMetadata("signalRConnectionId")?.ToString();
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var message = new WorkflowTestMessage
            {
                WorkflowInstanceId = context.WorkflowInstance.Id,
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,                
                Error = notification.Exception.InnerException?.InnerException?.ToString()
            };
            message.WorkflowStatus = message.Status = "Failed";

            await _workflowTestService.DispatchMessage(signalRConnectionId, message);
        }
    }
}