using Elsa.Models;

namespace Elsa.Dashboard.Extensions
{
    public static class WorkflowStatusExtensions
    {
        public static string GetStatusClass(this WorkflowStatus workflowStatus) =>
            workflowStatus switch
            {
                WorkflowStatus.Executing => "bg-info",
                WorkflowStatus.Faulted => "bg-warning",
                WorkflowStatus.Finished => "bg-success",
                WorkflowStatus.Aborted => "bg-info",
                _ => "bg-default",
            };
    }
}