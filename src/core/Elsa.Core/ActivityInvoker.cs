using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Extensions;
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
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Executing");
                var result = driver.ExecuteAsync(context, workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Executed");
                return result;
            });
        }

        public async Task<ActivityExecutionResult> ResumeAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) =>
            {
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Resuming");
                var result = driver.ResumeAsync(context, workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Resumed");
                return result;
            });
        }

        public async Task<ActivityExecutionResult> HaltedAsync(WorkflowExecutionContext workflowContext, IActivity activity, CancellationToken cancellationToken = default)
        {
            return await InvokeAsync(workflowContext, activity, (context, driver) =>
            {
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Halting");
                var result = driver.HaltedAsync(context, workflowContext, cancellationToken);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), "Halted");
                return result;
            });
        }

        private async Task<ActivityExecutionResult> InvokeAsync(
            WorkflowExecutionContext workflowContext,
            IActivity activity,
            Func<ActivityExecutionContext, IActivityDriver, Task<ActivityExecutionResult>> invokeAction)
        {
            var activityContext = workflowContext.CreateActivityExecutionContext(activity);
            var driver = driverRegistry.GetDriver(activity.TypeName);
          
            try
            {
                if (driver == null)
                    throw new WorkflowException($"No driver found for activity {activity.TypeName}");

                return await invokeAction(activityContext, driver);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while invoking activity {ActivityId} of workflow {WorkflowId}", activity.Id, workflowContext.Workflow.Id);
                workflowContext.Workflow.AddLogEntry(activity.Id, clock.GetCurrentInstant(), e.Message, true);
                return new FaultWorkflowResult(e);
            }
        }
    }
}