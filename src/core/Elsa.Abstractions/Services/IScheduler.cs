using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public interface IScheduler
    {
        Task<WorkflowExecutionContext> ResumeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity blockingActivity, object? input, CancellationToken cancellationToken = default);
        Task<WorkflowExecutionContext> RunAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);

        WorkflowExecutionContext CreateWorkflowExecutionContext(
            string workflowInstanceId,
            IEnumerable<IActivity> activities,
            IEnumerable<Connection> connections,
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            Variables? variables = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted);
    }
}