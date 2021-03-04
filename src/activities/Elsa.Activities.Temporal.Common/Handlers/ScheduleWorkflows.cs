using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.ActivityResults;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Temporal.Common.Handlers
{
    public class ScheduleWorkflows : INotificationHandler<WorkflowSuspended>
    {
        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            await notification.WorkflowExecutionContext.ExecuteRegisteredTasksAsync(nameof(ScheduleWorkflowResult), cancellationToken);
        }
    }
}