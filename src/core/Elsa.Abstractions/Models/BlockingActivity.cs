using Newtonsoft.Json;

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

        [JsonProperty(PropertyName = "activityId")]
        public string ActivityId { get; set; }

        [JsonProperty(PropertyName = "activityType")]
        public string ActivityType { get; set; }
    }
}