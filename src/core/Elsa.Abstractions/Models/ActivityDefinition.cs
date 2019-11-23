using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public static ActivityDefinition FromActivity(IActivity activity)
        {
            return new ActivityDefinition
            {
                Id = activity.Id,
                Type = activity.Type,
                State = activity.State
            };
        }

        public ActivityDefinition()
        {
            State = new JObject();
        }

        public string Id { get; set; }
        public string Type { get; set; }

        public string Name
        {
            get => State.ContainsKey(nameof(Name)) ? State[nameof(Name)].Value<string>() : default;
            set => State[nameof(Name)] = value;
        }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition()
        {
            Name = typeof(T).Name;
        }
    }
}