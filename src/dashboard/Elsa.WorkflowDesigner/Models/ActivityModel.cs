using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.WorkflowDesigner.Models
{
    public class ActivityModel
    {
        public ActivityModel()
        {
        }

        public ActivityModel(string id, string type, int left, int top, JObject state, bool blocking, bool executed, bool faulted, ActivityMessageModel? message = null)
        {
            Id = id;
            Type = type;
            Left = left;
            Top = top;
            State = state;
            Blocking = blocking;
            Executed = executed;
            Faulted = faulted;
            Message = message;
        }

        public ActivityModel(ActivityDefinition activityDefinition) : this(
            activityDefinition.Id,
            activityDefinition.Type,
            activityDefinition.Left,
            activityDefinition.Top,
            activityDefinition.State,
            false,
            false,
            false)
        {
        }
        
        public ActivityModel(ActivityDefinition activityDefinition, bool blocking, bool executed, bool faulted, ActivityMessageModel? message = null) : this(
            activityDefinition.Id,
            activityDefinition.Type,
            activityDefinition.Left,
            activityDefinition.Top,
            activityDefinition.State,
            blocking,
            executed,
            faulted,
            message)
        {
        }

        public string? Id { get; set; }
        public string? Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject? State { get; set; }
        public bool Blocking { get; set; }
        public bool Executed { get; set; }
        public bool Faulted { get; set; }
        public ActivityMessageModel? Message { get; set; }
    }
}