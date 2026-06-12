using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Handlers;

public class WriteWorkflowSuspendExecutionLog : INotificationHandler<WorkflowSuspended>
{
    private readonly ILogger _logger;

    public WriteWorkflowSuspendExecutionLog(ILogger<WriteWorkflowSuspendExecutionLog> logger)
    {
        _logger = logger;
    }

    public Task Handle(WorkflowSuspended notification, CancellationToken cancellationToken)
    {
        var context = notification.WorkflowExecutionContext;
        var blockingActivity = context.WorkflowInstance.BlockingActivities.FirstOrDefault();

        if (blockingActivity == null)
        {
            _logger.LogWarning("Workflow {WorkflowInstanceId} was suspended without blocking activities; skipping suspend execution log entry", context.WorkflowInstance.Id);
            return Task.CompletedTask;
        }

        context.WorkflowExecutionLog.AddEntry(context.WorkflowInstance.Id, blockingActivity.ActivityId, blockingActivity.ActivityType, nameof(WorkflowStatus.Suspended), null, null, null, null);
        return Task.CompletedTask;
    }
}
