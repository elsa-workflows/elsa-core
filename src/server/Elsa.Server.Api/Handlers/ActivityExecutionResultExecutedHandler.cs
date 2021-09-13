using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Services;
using MediatR;

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
            var signalRConnectionId = notification.ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.SignalRConnectionId;
            if (string.IsNullOrWhiteSpace(signalRConnectionId)) return;

            var message = new WorkflowTestMessage
            {
                SignalRConnectionId = signalRConnectionId,
                WorkflowInstanceId = notification.ActivityExecutionContext.WorkflowInstance.Id,
                CorrelationId = notification.ActivityExecutionContext.CorrelationId,
                ActivityId = notification.ActivityExecutionContext.ActivityId,
                Status = notification.ActivityExecutionContext.WorkflowExecutionContext.Status.ToString()
            };

            await _workflowTestService.DispatchMessage(message);
        }
    }
}