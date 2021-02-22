namespace Elsa.Models
{
    public class ActivityScope
    {
        public ActivityScope()
        {
        }

        public ActivityScope(string activityId)
        {
            ActivityId = activityId;
        }
        
        public string ActivityId { get; set; } = default!;
        public Variables Variables { get; set; } = new();
    }
}