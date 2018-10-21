using Flowsharp.Models;

namespace Flowsharp.Web.Persistence.Abstractions.Models
{
    public class WorkflowDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public Workflow Workflow { get; set; }
    }
}