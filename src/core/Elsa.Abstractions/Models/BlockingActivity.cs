namespace Elsa.Models
{
    public class BlockingActivity
    {
        public BlockingActivity()
        {
        }

        public BlockingActivity(string activityId, string activityType)
        {
            ActivityId = activityId;
            ActivityType = activityType;
        }
        
        public string ActivityId { get; set; }
        public string ActivityType { get; set; }
    }
}