using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;
using ProcessInstance = Elsa.Services.Models.ProcessInstance;

namespace Elsa.Extensions
{
    public static class ProcessExtensions
    {
        public static bool IsRunning(this ProcessInstance workflow) => workflow.Status == ProcessStatus.Running;

        public static bool IsCancelledOrFaulted(this ProcessInstance workflow) =>
            workflow.Status == ProcessStatus.Cancelled ||
            workflow.Status == ProcessStatus.Faulted;

        public static bool IsCompleted(this ProcessInstance workflow) => workflow.Status == ProcessStatus.Completed;

        public static LogEntry AddLogEntry(
            this ProcessInstance workflow, 
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