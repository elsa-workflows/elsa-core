using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Services.Models
{
    public class WorkflowFault : IWorkflowFault
    {
        public WorkflowFault(ActivityDefinition? activity = default, LocalizedString? message = default)
        {
            FaultedActivity = activity;
            Message = message;
        }

        public ActivityDefinition? FaultedActivity { get; }
        public LocalizedString? Message { get; }
    }
}