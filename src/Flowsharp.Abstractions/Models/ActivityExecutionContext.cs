using Flowsharp.Descriptors;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(ActivityType activityType, ActivityDescriptor activityDescriptor)
        {
            ActivityType = activityType;
            ActivityDescriptor = activityDescriptor;
            State = new JObject(activityType.State);
        }

        public ActivityType ActivityType { get; }
        public ActivityDescriptor ActivityDescriptor { get; }
        public JObject State { get; set; }
    }
}
