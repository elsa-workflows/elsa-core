using Elsa.Models;

namespace Elsa.Web.Shapes
{
    public class ActivityDesign : ActivityShape
    {
        public ActivityDesign(IActivity activity, ActivityDescriptor activityDescriptor, ActivityDesignerDescriptor activityDesignerDescriptor) 
            : base(activity, activityDescriptor, activityDesignerDescriptor, "Activity_Design")
        {
        }
    }
}