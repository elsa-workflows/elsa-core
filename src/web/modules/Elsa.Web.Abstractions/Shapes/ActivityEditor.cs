using System.Collections.Generic;
using System.Linq;
using Elsa.Models;

namespace Elsa.Web.Shapes
{
    public class ActivityEditor : ActivityShape
    {
        public IEnumerable<ActivityPropertyEditor> PropertyEditorShapes { get; }

        public ActivityEditor(IActivity activity,
            ActivityDescriptor activityDescriptor,
            ActivityDesignerDescriptor activityDesignerDescriptor, 
            IEnumerable<ActivityPropertyEditor> propertyEditorShapes) : base(activity, activityDescriptor, activityDesignerDescriptor, "Activity_Editor")
        {
            PropertyEditorShapes = propertyEditorShapes.ToList();
        }
    }
}