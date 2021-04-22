using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecutionResultExecuted>, INotificationHandler<ActivityFaulted>
    {
        private readonly IWorkflowExecutionLog _workflowExecutionLog;
        public WriteWorkflowExecutionLog(IWorkflowExecutionLog workflowExecutionLog) => _workflowExecutionLog = workflowExecutionLog;
        
        public async Task Handle(ActivityExecuting notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            
            var data = new
            {
                Input = notification.ActivityExecutionContext.Input,
                State = activityExecutionContext.GetActivityData()
            };
            
            await WriteEntryAsync(notification.Resuming ? "Resuming" : "Executing", default, activityExecutionContext, data, cancellationToken);
        }

        public async Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            
            var data = new
            {
                Output = activityExecutionContext.Output,
                Outcomes = activityExecutionContext.Outcomes,
                State = activityExecutionContext.GetActivityData()
            };

            var resuming = activityExecutionContext.Resuming;
            await WriteEntryAsync(resuming ? "Resumed" : "Executed", default, activityExecutionContext, data, cancellationToken);
        }

        public async Task Handle(ActivityFaulted notification, CancellationToken cancellationToken)
        {
            var exception = notification.Exception;

            var data = new
            {
                exception.Message,
                exception.StackTrace
            };

            await WriteEntryAsync("Faulted", exception.Message, notification.ActivityExecutionContext, data, cancellationToken);
        }

        private async Task WriteEntryAsync(string eventName, string? message, ActivityExecutionContext activityExecutionContext, object? data, CancellationToken cancellationToken)
        {
            var workflowInstance = activityExecutionContext.WorkflowInstance;
            var activityBlueprint = activityExecutionContext.ActivityBlueprint;
            await _workflowExecutionLog.AddEntryAsync(eventName, workflowInstance, activityBlueprint, message, data, default, cancellationToken);
        }
    }
}