using System.Reflection;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Shapes
{
    public class ActivityPropertyEditor : ShapeViewModel
    {
        public ActivityPropertyEditor(PropertyInfo propertyInfo, object propertyValue) : base("ActivityProperty_Editor")
        {
            PropertyInfo = propertyInfo;
            PropertyValue = propertyValue;
        }
        
        public PropertyInfo PropertyInfo { get; }
        public object PropertyValue { get; }

    }
}