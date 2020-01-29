using Elsa.Models;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowDefinitionListItemModel
    {
        public WorkflowDefinitionVersion WorkflowDefinition { get; set; }
        public int ExecutingCount { get; set; }
        public int FaultedCount { get; set; }
        public int AbortedCount { get; set; }
        public int FinishedCount { get; set; }
    }
}