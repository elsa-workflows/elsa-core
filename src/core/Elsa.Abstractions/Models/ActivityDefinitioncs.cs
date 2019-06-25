using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public static ActivityDefinition FromActivity(IActivity activity)
        {
            return new ActivityDefinition(activity.Id, activity.TypeName, activity.State);
        }
        
        public ActivityDefinition(string id, string typeName, JObject state)
        {
            Id = id;
            TypeName = typeName;
            State = new JObject(state);
        }
        
        public string Id { get; set; }
        public string TypeName { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition(string id, JObject state) : base(id, typeof(T).Name, state)
        {
        }
        
        public ActivityDefinition(string id, object state) : base(id, typeof(T).Name, JObject.FromObject(state))
        {
        }
    }
}