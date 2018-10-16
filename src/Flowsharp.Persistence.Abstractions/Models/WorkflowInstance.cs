using Flowsharp.Models;

namespace Flowsharp.Persistence.Models
{
    public class WorkflowInstance
    {
        public string Id { get; set; }
        public string WorkflowDefinitionId { get; set; }
        public Workflow Workflow { get; set; }
    }
}