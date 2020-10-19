using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;
using NodaTime;

namespace Elsa.Handlers
{
    public class AppendExecutionLog : INotificationHandler<ActivityExecuted>
    {
        private readonly IClock _clock;
        public AppendExecutionLog(IClock clock) => _clock = clock;

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.ExecutionLog.Add(new ExecutionLogEntry(notification.Activity.Id, _clock.GetCurrentInstant()));
            return Task.CompletedTask;
        }
    }
}