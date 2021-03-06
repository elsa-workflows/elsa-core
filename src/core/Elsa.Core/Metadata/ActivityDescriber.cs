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

        public ActivityDescriptor? Describe(Type activityType)
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

            return new ActivityDescriptor
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

        private IEnumerable<ActivityPropertyDescriptor> DescribeProperties(Type activityType)
        {
            var properties = activityType.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var activityProperty = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityProperty == null)
                    continue;

                var options = activityProperty.OptionsProvider is not null and not "" ? GetOptions(activityType, activityProperty.OptionsProvider) : activityProperty.Options;

                yield return new ActivityPropertyDescriptor
                (
                    (activityProperty.Name ?? propertyInfo.Name).Pascalize(),
                    (activityProperty.UIHint ?? InferPropertyUIHint(propertyInfo)),
                    activityProperty.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityProperty.Hint,
                    options
                );
            }
        }

        private object? GetOptions(Type activityType, string providerMethodName)
        {
            var providerMethod = activityType.GetMethod(providerMethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (providerMethod == null)
                throw new MissingMethodException(activityType.Name, providerMethodName);

            var options = providerMethod.Invoke(null, new object[0]);
            return options;
        }

        private string InferPropertyUIHint(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.PropertyType;

            if (type == typeof(bool) || type == typeof(bool?))
                return ActivityPropertyUIHints.Checkbox;

            if (type == typeof(string))
                return ActivityPropertyUIHints.SingleLine;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ActivityPropertyUIHints.Dropdown;

            return ActivityPropertyUIHints.SingleLine;
        }
    }
}