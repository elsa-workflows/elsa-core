using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;

namespace Elsa.Handlers;

public class WriteWorkflowSuspendExecutionLog : INotificationHandler<WorkflowSuspended>
{
    public Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
    {
        var context = notification.WorkflowExecutionContext;
        var blockingActivity = context.WorkflowInstance.BlockingActivities.First();
        context.WorkflowExecutionLog.AddEntry(context.WorkflowInstance.Id, blockingActivity.ActivityId, blockingActivity.ActivityType, nameof(WorkflowStatus.Suspended), null, null, null, null);
        return Task.CompletedTask;
    }
}
