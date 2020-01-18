using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using MediatR;
using NodaTime;

namespace Elsa.Messages.Domain.Handlers
{
    public class WorkflowExecutionLogEventHandler : INotificationHandler<ActivityExecuted>
    {
        private readonly IClock clock;

        public WorkflowExecutionLogEventHandler(IClock clock)
        {
            this.clock = clock;
        }

        public Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.ExecutionLog.Add(new ExecutionLogEntry(notification.Activity, clock.GetCurrentInstant()));
            return Task.CompletedTask;
        }
    }
}