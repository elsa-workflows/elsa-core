using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using MediatR;
using NodaTime;

namespace Elsa.Handlers
{
    public class UpdateWorkflowTimeStamps : INotificationHandler<WorkflowExecuted>, INotificationHandler<WorkflowCompleted>, INotificationHandler<WorkflowCancelled>, INotificationHandler<WorkflowFaulted>
    {
        private readonly IClock _clock;

        public UpdateWorkflowTimeStamps(IClock clock)
        {
            _clock = clock;
        }

        public Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.WorkflowInstance.LastExecutedAt = _clock.GetCurrentInstant();
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.WorkflowInstance.FinishedAt = _clock.GetCurrentInstant();
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowCancelled notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.WorkflowInstance.CancelledAt = _clock.GetCurrentInstant();
            return Task.CompletedTask;
        }

        public Task Handle(WorkflowFaulted notification, CancellationToken cancellationToken)
        {
            notification.WorkflowExecutionContext.WorkflowInstance.FaultedAt = _clock.GetCurrentInstant();
            return Task.CompletedTask;
        }
    }
}