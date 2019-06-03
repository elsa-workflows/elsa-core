using Elsa.Models;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Shapes
{
    public class ActivityShape : ShapeViewModel
    {
        protected ActivityShape(IActivity activity, ActivityDescriptor activityDescriptor, ActivityDesignerDescriptor activityDesignerDescriptor, string shapeType) : base(shapeType)
        {
            Activity = activity;
            ActivityDescriptor = activityDescriptor;
            ActivityDesignerDescriptor = activityDesignerDescriptor;
        }
        
        public IActivity Activity { get; }
        public ActivityDescriptor ActivityDescriptor { get; }
        public ActivityDesignerDescriptor ActivityDesignerDescriptor { get; }
    }
}