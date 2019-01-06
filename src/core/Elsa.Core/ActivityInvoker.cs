using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa
{
    public class ActivityInvoker : IActivityInvoker
    {
        private readonly IActivityDriverRegistry driverRegistry;
        private readonly IClock clock;
        private readonly ILogger logger;

        public ActivityInvoker(IActivityDriverRegistry driverRegistry, IClock clock, ILogger<ActivityInvoker> logger)
        {
            this.driverRegistry = driverRegistry;
            this.clock = clock;
            this.logger = logger;
        }

        public async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) =>
            {
                workflowContext.Workflow.ExecutionLog.Add(context.LogEntry);
                context.LogEntry.ExecutedAt = clock.GetCurrentInstant();
                return driver.ExecuteAsync(context, workflowContext, cancellationToken);
            });
        }

        public async Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) =>
            {
                context.LogEntry.ResumedAt = clock.GetCurrentInstant();
                return driver.ResumeAsync(context, workflowContext, cancellationToken);
            });
        }

        public async Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) =>
            {
                context.LogEntry.HaltedAt = clock.GetCurrentInstant();
                return driver.HaltedAsync(context, workflowContext, cancellationToken);
            });
        }

        private async Task<ActivityExecutionResult> InvokeAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<ActivityExecutionContext, IActivityDriver, Task<ActivityExecutionResult>> invokeAction)
        {
            var activityContext = workflowContext.CreateActivityExecutionContext(activity);
            var driver = driverRegistry.GetDriver(activity.Name);
          
            try
            {
                if (driver == null)
                    throw new WorkflowException($"No driver found for activity {activity.Name}");

                return await invokeAction(activityContext, driver);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while invoking activity {ActivityId} of workflow {WorkflowId}", activity.Id, workflowContext.Workflow.Metadata.Id);
                activityContext.LogEntry.Fault = new ActivityFault(e.Message);
                return new FaultWorkflowResult(activityContext.LogEntry.Fault.Message, clock.GetCurrentInstant());
            }
        }
    }
}