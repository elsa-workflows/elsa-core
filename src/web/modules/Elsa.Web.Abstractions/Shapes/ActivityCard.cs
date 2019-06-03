using Elsa.Models;

namespace Elsa.Web.Shapes
{
    public class ActivityCard : ActivityShape
    {
        public ActivityCard(IActivity activity, ActivityDescriptor activityDescriptor, ActivityDesignerDescriptor activityDesignerDescriptor) 
            : base(activity, activityDescriptor, activityDesignerDescriptor, "Activity_Card")
        {
        }
    }
}