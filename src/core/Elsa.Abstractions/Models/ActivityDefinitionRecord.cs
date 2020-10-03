using Elsa.Services;

namespace Elsa.Models
{
    public class ActivityDefinitionRecord
    {
        public static ActivityDefinitionRecord FromActivity(IActivity activity)
        {
            return new ActivityDefinitionRecord
            {
                Id = activity.Id,
                Type = activity.Type,
                //State = activity.State,
                Name = activity.Name,
                DisplayName = activity.DisplayName
            };
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int? Left { get; set; }
        public int? Top { get; set; }
        public bool PersistWorkflow { get; set; }
        public Variables? State { get; set; }
    }

    public class ActivityDefinitionRecord<T> : ActivityDefinitionRecord where T : IActivity
    {
        public ActivityDefinitionRecord()
        {
            Type = typeof(T).Name;
        }
    }
}