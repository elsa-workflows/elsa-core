using Elsa.Extensions;
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
            State = new Variables();
        }

        public string Id { get; set; }
        public string Type { get; set; }

        public string Name
        {
            get => State.GetState<string>();
            set => State.SetState(value);
        }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public Variables State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition()
        {
            Type = typeof(T).Name;
        }
    }
}