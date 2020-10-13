using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Services.Models
{
    public class WorkflowFault : IWorkflowFault
    {
        public WorkflowFault(string? activityId = default, LocalizedString? message = default)
        {
            FaultedActivityId = activityId;
            Message = message;
        }

        public string? FaultedActivityId { get; }
        public LocalizedString? Message { get; }
    }
}