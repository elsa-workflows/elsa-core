using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecutionResultExecuted>, INotificationHandler<ActivityFaulted>
    {
        private readonly IWorkflowExecutionLog _workflowExecutionLog;
        private readonly IMapper _mapper;

        public WriteWorkflowExecutionLog(IWorkflowExecutionLog workflowExecutionLog, IMapper mapper)
        {
            _workflowExecutionLog = workflowExecutionLog;
            _mapper = mapper;
        }

        public async Task Handle(ActivityExecuting notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            
            await WriteEntryAsync(notification.Resuming ? "Resuming" : "Executing", default, activityExecutionContext, null, cancellationToken);
        }

        public async Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            
            var data = new
            {
                Outcomes = activityExecutionContext.Outcomes,
            };

            var resuming = activityExecutionContext.Resuming;
            await WriteEntryAsync(resuming ? "Resumed" : "Executed", default, activityExecutionContext, data, cancellationToken);
        }

        public async Task Handle(ActivityFaulted notification, CancellationToken cancellationToken)
        {
            var exception = notification.Exception;
            var exceptionModel = _mapper.Map<SimpleException>(exception);

            var data = new
            {
                Exception = exceptionModel
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