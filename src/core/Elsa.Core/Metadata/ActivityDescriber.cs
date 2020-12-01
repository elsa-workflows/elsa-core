using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Design;
using Humanizer;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public class ActivityDescriber : IActivityDescriber
    {
        private readonly IEnumerable<IActivityPropertyOptionsProvider> _optionsProviders;

        public ActivityDescriber(IEnumerable<IActivityPropertyOptionsProvider> optionsProviders)
        {
            _optionsProviders = optionsProviders;
        }
        
        public ActivityInfo? Describe(Type activityType)
        {
            var browsableAttribute = activityType.GetCustomAttribute<BrowsableAttribute>();
            var isBrowsable = browsableAttribute == null || browsableAttribute.Browsable;
            var activityAttribute = activityType.GetCustomAttribute<ActivityAttribute>(false);
            var typeName = activityAttribute?.Type ?? activityType.Name;
            var displayName = activityAttribute?.DisplayName ?? activityType.Name.Humanize(LetterCasing.Title);
            var description = activityAttribute?.Description;
            var category = activityAttribute?.Category ?? "Miscellaneous";
            var traits = activityAttribute?.Traits ?? ActivityTraits.Action;
            var outcomes = activityAttribute?.Outcomes ?? new[] { OutcomeNames.Done };
            var properties = DescribeProperties(activityType);

            return new ActivityInfo
            {
                Type = typeName.Pascalize(),
                DisplayName = displayName,
                Description = description,
                Category = category,
                Traits = traits,
                Properties = properties.ToArray(),
                Outcomes = outcomes,
                Browsable = isBrowsable
            };
        }

        private IEnumerable<ActivityPropertyInfo> DescribeProperties(Type activityType)
        {
            var properties = activityType.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var activityProperty = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityProperty == null)
                    continue;

                yield return new ActivityPropertyInfo
                (
                    (activityProperty.Name ?? propertyInfo.Name).Camelize(),
                    (activityProperty.Type ?? DeterminePropertyType(propertyInfo)).Pascalize(),
                    activityProperty.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityProperty.Hint,
                    GetPropertyTypeOptions(propertyInfo)
                );
            }
        }
        
        private JObject GetPropertyTypeOptions(PropertyInfo propertyInfo)
        {
            var options = new JObject();

            foreach (var provider in _optionsProviders.Where(x => x.SupportsProperty(propertyInfo))) 
                provider.SupplyOptions(propertyInfo, options);

            return options;
        }

        private string DeterminePropertyType(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.PropertyType;

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