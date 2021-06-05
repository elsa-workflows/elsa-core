using System.Collections;
using System.Reflection;
using AutoMapper.Internal;
using Elsa.Attributes;
using Elsa.Design;

namespace Elsa.Metadata
{
    public class ActivityPropertyUIHintResolver : IActivityPropertyUIHintResolver
    {
        public string GetUIHint(PropertyInfo activityPropertyInfo)
        {
            var activityPropertyAttribute = activityPropertyInfo.GetCustomAttribute<ActivityInputAttribute>();

            if (activityPropertyAttribute.UIHint != null)
                return activityPropertyAttribute.UIHint;
            
            var type = activityPropertyInfo.PropertyType;

            if (type == typeof(bool) || type == typeof(bool?))
                return ActivityInputUIHints.Checkbox;

            if (type == typeof(string))
                return ActivityInputUIHints.SingleLine;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ActivityInputUIHints.Dropdown;

            if (type.IsEnum || type.IsNullableType() && type.GetTypeOfNullable().IsEnum)
                return ActivityInputUIHints.Dropdown;

            return ActivityInputUIHints.SingleLine;
        }
    }
}