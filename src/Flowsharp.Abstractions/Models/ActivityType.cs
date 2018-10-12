using Newtonsoft.Json.Linq;

namespace Flowsharp.Models
{
    public class ActivityType
    {
        public ActivityType(string id, string name, JObject state = null)
        {
            Id = id;
            Name = name;
            State = state ?? new JObject();
        }

        public string Id { get; }
        public string Name { get; }
        public JObject State { get; }
    }
}
