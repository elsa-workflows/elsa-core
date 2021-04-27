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
            var activityPropertyAttribute = activityPropertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

            if (activityPropertyAttribute.UIHint != null)
                return activityPropertyAttribute.UIHint;
            
            var type = activityPropertyInfo.PropertyType;

            if (type == typeof(bool) || type == typeof(bool?))
                return ActivityPropertyUIHints.Checkbox;

            if (type == typeof(string))
                return ActivityPropertyUIHints.SingleLine;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ActivityPropertyUIHints.Dropdown;

            if (type.IsEnum || type.IsNullableType() && type.GetTypeOfNullable().IsEnum)
                return ActivityPropertyUIHints.Dropdown;

            return ActivityPropertyUIHints.SingleLine;
        }
    }
}