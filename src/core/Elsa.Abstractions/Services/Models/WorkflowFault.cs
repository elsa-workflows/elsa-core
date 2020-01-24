using Microsoft.Extensions.Localization;

namespace Elsa.Services.Models
{
    public class WorkflowFault
    {
        public WorkflowFault(IActivity? activity = default, LocalizedString? message = default)
        {
            FaultedActivity = activity;
            Message = message;
        }

        public IActivity? FaultedActivity { get; }
        public LocalizedString? Message { get; }
    }
}