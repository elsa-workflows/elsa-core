using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.WorkflowEventHandlers
{
    public class ActivityLoggingWorkflowEventHandler : WorkflowEventHandlerBase
    {
        private readonly IClock clock;

        public ActivityLoggingWorkflowEventHandler(IClock clock)
        {
            this.clock = clock;
        }

        protected override void ActivityExecuted(WorkflowExecutionContext workflowExecutionContext, IActivity activity)
        {
            var timeStamp = clock.GetCurrentInstant();
            workflowExecutionContext.Workflow.ExecutionLog.Add(
                new LogEntry(activity.Id, timeStamp, $"Successfully executed at {timeStamp}"));
        }

        protected override void ActivityFaulted(
            WorkflowExecutionContext workflowExecutionContext,
            IActivity activity,
            string message)
        {
            workflowExecutionContext.Workflow.ExecutionLog.Add(
                new LogEntry(activity.Id, clock.GetCurrentInstant(), message, true));
        }
    }
}