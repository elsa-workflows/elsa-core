using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using MediatR;
using NodaTime;

namespace Elsa.Messaging.Domain.Handlers
{
    public class WorkflowExecutionLogEventHandler : INotificationHandler<ActivityExecuted>
    {
        private readonly IClock _clock;
        public WorkflowExecutionLogEventHandler(IClock clock) => _clock = clock;

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.ExecutionLog.Add(new ExecutionLogEntry(notification.Activity.Id, _clock.GetCurrentInstant()));
            return Task.CompletedTask;
        }
    }
}