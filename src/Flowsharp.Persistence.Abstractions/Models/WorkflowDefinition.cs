using Flowsharp.Models;

namespace Flowsharp.Persistence.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public Workflow Workflow { get; set; }
    }
}