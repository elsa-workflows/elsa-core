using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using MediatR;
using NodaTime;

namespace Elsa.Messaging.Domain.Handlers
{
    public class WorkflowExecutionLogEventHandler : INotificationHandler<ActivityExecuted>
    {
        private readonly IClock _clock;

        public WorkflowExecutionLogEventHandler(IClock clock)
        {
            this._clock = clock;
        }

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.ExecutionLog.Add(new ExecutionLogEntry(notification.Activity, _clock.GetCurrentInstant()));
            return Task.CompletedTask;
        }
    }
}