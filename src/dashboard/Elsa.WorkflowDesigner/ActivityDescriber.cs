using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services.Models;
using Elsa.WorkflowDesigner.Models;
using Humanizer;

namespace Elsa.WorkflowDesigner
{
    public static class ActivityDescriber
    {
        public static ActivityDefinition Describe<T>() where T : IActivity
        {
            return Describe(typeof(T));
        }

        public static ActivityDefinition Describe(Type activityType)
        {
            var activityDefinitionAttribute = activityType.GetCustomAttribute<ActivityDefinitionAttribute>();
            var activityDesignerAttribute = activityType.GetCustomAttribute<ActivityDefinitionDesignerAttribute>();
            var typeName = activityDefinitionAttribute?.Type ?? activityType.Name;
            
            var displayName =
                activityDefinitionAttribute?.DisplayName ??
                activityType.Name.Humanize(LetterCasing.Title);
            
            var description = activityDefinitionAttribute?.Description;
            var category = activityDefinitionAttribute?.Category ?? "Miscellaneous";
            var icon = activityDefinitionAttribute?.Icon;
            var designerDescription = activityDesignerAttribute?.Description;
            var designerOutcomes = activityDesignerAttribute?.Outcomes ?? new[] { OutcomeNames.Done };
            var properties = DescribeProperties(activityType);

            return new ActivityDefinition
            {
                Type = typeName.Pascalize(),
                DisplayName = displayName,
                Description = description,
                Category = category,
                Icon = icon,
                Properties = properties.ToArray(),
                Designer = new ActivityDesignerSettings
                {
                    Description = designerDescription,
                    Outcomes = designerOutcomes
                }
            };
        }

        private static IEnumerable<ActivityPropertyDescriptor> DescribeProperties(Type activityType)
        {
            var properties = activityType.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var activityProperty = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityProperty == null)
                    yield break;

                yield return new ActivityPropertyDescriptor
                (
                    (activityProperty.Name ?? propertyInfo.Name).Camelize(),
                    (activityProperty.Type ?? DeterminePropertyType(propertyInfo)).Camelize(),
                    activityProperty.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityProperty.Hint,
                    GetPropertyTypeOptions(propertyInfo)
                );
            }
        }
        
        private static object GetPropertyTypeOptions(PropertyInfo propertyInfo)
        {
            var optionsAttribute = propertyInfo.GetCustomAttribute<ActivityPropertyOptionsAttribute>();

            return optionsAttribute?.GetOptions() ?? new object();
        }

        private static string DeterminePropertyType(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.PropertyType;

            if (typeof(IWorkflowExpression).IsAssignableFrom(type))
                return ActivityPropertyTypes.Expression;

            if (type == typeof(bool) || type == typeof(bool?))
                return ActivityPropertyTypes.Boolean;
            
            if (type == typeof(string))
                return ActivityPropertyTypes.Text;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ActivityPropertyTypes.List;

            return ActivityPropertyTypes.Text;
        }
    }
}