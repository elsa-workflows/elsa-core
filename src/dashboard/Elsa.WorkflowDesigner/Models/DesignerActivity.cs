using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.WorkflowDesigner.Models
{
    public class DesignerActivity
    {
        public DesignerActivity()
        {
        }

        public DesignerActivity(string id, string type, int left, int top, JObject state)
        {
            Id = id;
            Type = type;
            Left = left;
            Top = top;
            State = state;
        }

        public DesignerActivity(ActivityDefinition activityDefinition) : this(
            activityDefinition.Id,
            activityDefinition.Type,
            activityDefinition.Left,
            activityDefinition.Top,
            activityDefinition.State)
        {
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }
}