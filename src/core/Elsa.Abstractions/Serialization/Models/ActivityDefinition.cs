using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Models
{
    public class ActivityDefinition
    {
        public ActivityDefinition()
        {
        }

        public ActivityDefinition(string id, string typeName, JObject state = null)
        {
            Id = id;
            TypeName = typeName;
            State = state;
        }
        
        public ActivityDefinition(string id, string typeName, object state = null)
        {
            Id = id;
            TypeName = typeName;
            State =  state != null ? JObject.FromObject(state) : default;
        }
        
        public string Id { get; set; }
        public string TypeName { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition(string id, object state = null) : base(id, typeof(T).Name, state)
        {
        }
    }
}