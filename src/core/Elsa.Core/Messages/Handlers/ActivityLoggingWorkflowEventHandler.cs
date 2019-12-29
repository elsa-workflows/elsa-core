using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Messages.Handlers
{
    public class ActivityLoggingWorkflowEventHandler : WorkflowEventHandlerBase
    {
        private readonly IClock clock;

        public ActivityLoggingWorkflowEventHandler(IClock clock)
        {
            this.clock = clock;
        }

        protected override void ActivityExecuted(ProcessExecutionContext processExecutionContext, IActivity activity)
        {
            var timeStamp = clock.GetCurrentInstant();
            processExecutionContext.ProcessInstance.ExecutionLog.Add(
                new LogEntry(activity.Id, timeStamp, $"Successfully executed at {timeStamp}"));
        }

        protected override void ActivityFaulted(
            ProcessExecutionContext processExecutionContext,
            IActivity activity,
            string message)
        {
            processExecutionContext.ProcessInstance.ExecutionLog.Add(
                new LogEntry(activity.Id, clock.GetCurrentInstant(), message, true));
        }
    }
}