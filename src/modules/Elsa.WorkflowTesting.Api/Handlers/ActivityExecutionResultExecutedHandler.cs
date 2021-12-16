using Elsa.ActivityResults;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.WorkflowTesting.Messages;
using Elsa.WorkflowTesting.Services;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowTesting.Api.Handlers
{
    public class ActivityExecutionResultExecutedHandler : INotificationHandler<ActivityExecutionResultExecuted>, INotificationHandler<ActivityExecutionFailed>, INotificationHandler<ActivityFaulted>
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

            if (context.Input != null)
                data["Input"] = JToken.FromObject(context.Input);


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

        public Task Handle(ActivityExecutionFailed notification, CancellationToken cancellationToken) => HandleFaultedExecutionAsync(notification.ActivityExecutionContext, notification.Exception);
        public Task Handle(ActivityFaulted notification, CancellationToken cancellationToken) => HandleFaultedExecutionAsync(notification.ActivityExecutionContext, notification.Exception);

        private async Task HandleFaultedExecutionAsync(ActivityExecutionContext context, Exception exception)
        {
            var signalRConnectionId = context.WorkflowExecutionContext.WorkflowInstance.GetMetadata("signalRConnectionId")?.ToString();
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var innerException = exception;

            while (innerException?.InnerException != null) innerException = innerException.InnerException;

            var message = new WorkflowTestMessage
            {
                WorkflowInstanceId = context.WorkflowInstance.Id,
                CorrelationId = context.CorrelationId,
                ActivityId = context.ActivityId,
                Error = innerException?.ToString()
            };

            message.WorkflowStatus = message.Status = "Failed";
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
                    status = outcomeResult.Outcomes.First();
                    break;
                case FaultResult:
                    status = "Failed";
                    break;
                case CombinedResult combinedResult:
                    status = GetExecutionResult(combinedResult.Results.First());
                    break;
            }

            return status;
        }
    }
}