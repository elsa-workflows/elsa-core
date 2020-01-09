using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Extensions
{
    public static class WorkflowInstanceExtensions
    {
        public static bool IsRunning(this WorkflowInstance workflow) => workflow.Status == WorkflowStatus.Running;

        public static bool IsCancelledOrFaulted(this WorkflowInstance workflow) =>
            workflow.Status == WorkflowStatus.Cancelled ||
            workflow.Status == WorkflowStatus.Faulted;

        public static bool IsCompleted(this WorkflowInstance workflow) => workflow.Status == WorkflowStatus.Completed;

        public static LogEntry AddLogEntry(
            this WorkflowInstance workflow, 
            IActivity activity, 
            Instant instant, 
            string message,
            bool faulted = false)
        {
            var entry = new LogEntry(activity.Id, instant, message, faulted);
            workflow.ExecutionLog.Add(entry);
            return entry;
        }
    }
}