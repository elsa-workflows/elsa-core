using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityDefinition
    {
        public static ActivityDefinition FromActivity(IActivity activity)
        {
            return new ActivityDefinition(activity.Id, activity.Type, activity.State, 0, 0);
        }

        public ActivityDefinition()
        {
            State = new JObject();
        }

        public ActivityDefinition(string id, string type, JObject state, int left = 0, int top = 0)
        {
            Id = id;
            Type = type;
            Left = left;
            Top = top;
            State = new JObject(state);
        }

        public string Id { get; set; }
        public string Type { get; set; }

        public string Name
        {
            get => State.ContainsKey(nameof(Name).ToLower()) ? State[nameof(Name).ToLower()].Value<string>() : default;
            set => State[nameof(Name).ToLower()] = value;
        }


        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition(string id, JObject state, int left = 0, int top = 0) : base(
            id,
            typeof(T).Name,
            state,
            left,
            top)
        {
        }

        public ActivityDefinition(string id, object state, int left = 0, int top = 0) : base(
            id,
            typeof(T).Name,
            JObject.FromObject(state),
            left,
            top)
        {
        }
    }
}