using System.Collections.Generic;

namespace Elsa.WorkflowDesigner.Models
{
    public class DesignerWorkflow
    {
        public ICollection<DesignerActivity> Activities { get; set; } = new List<DesignerActivity>();
        public ICollection<DesignerConnection> Connections { get; set; } = new List<DesignerConnection>();
    }
}