using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public static ActivityDefinition FromActivity(IActivity activity)
        {
            return new ActivityDefinition(activity.Id, activity.Type, activity.State);
        }

        public ActivityDefinition()
        {
        }
        
        public ActivityDefinition(string id, string type, JObject state)
        {
            Id = id;
            Type = type;
            State = new JObject(state);
        }
        
        public string Id { get; set; }
        public string Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
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