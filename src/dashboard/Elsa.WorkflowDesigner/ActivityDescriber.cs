using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Services.Models;
using Elsa.WorkflowDesigner.Models;

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
            var displayName = activityDefinitionAttribute?.DisplayName ?? activityType.Name;
            var description = activityDefinitionAttribute?.Description;
            var category = activityDefinitionAttribute?.Category ?? "Miscellaneous";
            var designerDescription = activityDesignerAttribute?.Description;
            var designerOutcomes = activityDesignerAttribute?.Outcomes ?? new[] { OutcomeNames.Done };
            var properties = DescribeProperties(activityType);
            
            return new ActivityDefinition
            {
                Type = typeName,
                DisplayName = displayName,
                Description = description,
                Category = category,
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
                yield return new ActivityPropertyDescriptor
                {
                    Name = propertyInfo.Name,
                    Label = propertyInfo.Name,
                    Type = "text",
                    Options = new { }
                };
            }
        }
    }
}