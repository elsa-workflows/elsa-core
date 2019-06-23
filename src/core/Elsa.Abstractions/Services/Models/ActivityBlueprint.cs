using Newtonsoft.Json.Linq;

namespace Elsa.Services.Models
{
    public class ActivityBlueprint
    {
        public static ActivityBlueprint FromActivity(IActivity activity)
        {
            return new ActivityBlueprint(activity.Id, activity.TypeName, activity.State);
        }
        
        public ActivityBlueprint(string id, string typeName, JObject state)
        {
            Id = id;
            TypeName = typeName;
            State = new JObject(state);
        }
        
        public string Id { get; set; }
        public string TypeName { get; set; }
        public JObject State { get; set; }
    }
}