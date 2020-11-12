namespace Elsa.Client.Models
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

        public string ActivityId { get; set; } = default!;
        public string ActivityType { get; set; }= default!;
        public string? Tag { get; set; }
    }
}