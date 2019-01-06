using Elsa.Models;

namespace Elsa.Web.Management.ViewModels
{
    public class WorkflowSummaryViewModel
    {
        public Workflow Workflow { get; set; }
        public int NumHalted { get; set; }
        public int NumFaulted { get; set; }
        public int NumFinished { get; set; }
    }
}