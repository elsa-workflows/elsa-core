using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using MediatR;

namespace Elsa.Handlers
{
    public class ExecuteWorkflowsPostSuspensionTasks : INotificationHandler<WorkflowSuspended>
    {
        public async Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
        {
            await notification.WorkflowExecutionContext.ProcessRegisteredTasksAsync(cancellationToken);
        }
    }
}