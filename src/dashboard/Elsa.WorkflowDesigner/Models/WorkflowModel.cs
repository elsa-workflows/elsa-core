using System.Collections.Generic;

namespace Elsa.WorkflowDesigner.Models
{
    public class WorkflowModel
    {
        public ICollection<ActivityModel> Activities { get; set; } = new List<ActivityModel>();
        public ICollection<ConnectionModel> Connections { get; set; } = new List<ConnectionModel>();
    }
}