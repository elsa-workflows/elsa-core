using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;
using WorkflowExecutionScope = Elsa.Services.Models.WorkflowExecutionScope;

namespace Elsa.Services
{
    public interface IScheduler
    {
        Task<WorkflowExecutionContext> ScheduleActivityAsync(
            IActivity activity,
            object? input = default,
            CancellationToken cancellationToken = default);

        Task<WorkflowExecutionContext> ResumeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity blockingActivity, object? input, CancellationToken cancellationToken = default);
        Task<WorkflowExecutionContext> RunAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);

        WorkflowExecutionContext CreateWorkflowExecutionContext(
            string workflowInstanceId, 
            IEnumerable<ScheduledActivity>? scheduledActivities = default,
            IEnumerable<IActivity>? blockingActivities = default,
            IEnumerable<WorkflowExecutionScope>? scopes = default,
            WorkflowStatus status = WorkflowStatus.Running,
            WorkflowPersistenceBehavior persistenceBehavior = WorkflowPersistenceBehavior.WorkflowExecuted);
    }
}