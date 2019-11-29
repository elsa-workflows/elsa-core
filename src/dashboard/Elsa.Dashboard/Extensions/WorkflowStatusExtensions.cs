using Elsa.Models;

namespace Elsa.Dashboard.Extensions
{
    public static class WorkflowStatusExtensions
    {
        public static string GetStatusClass(this WorkflowStatus workflowStatus) =>
            workflowStatus switch
            {
                WorkflowStatus.Running => "bg-info",
                WorkflowStatus.Faulted => "bg-warning",
                WorkflowStatus.Completed => "bg-success",
                WorkflowStatus.Cancelled => "bg-info",
                _ => "bg-default",
            };
    }
}