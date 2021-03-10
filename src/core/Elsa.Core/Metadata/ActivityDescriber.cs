using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Design;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public class ActivityDescriber : IActivityDescriber
    {
        private readonly IServiceProvider _serviceProvider;

        public ActivityDescriber(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ActivityDescriptor? Describe(Type activityType)
        {
            var browsableAttribute = activityType.GetCustomAttribute<BrowsableAttribute>(false);
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
            using var scope = _serviceProvider.CreateScope();

            foreach (var propertyInfo in properties)
            {
                var activityPropertyAttribute = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityPropertyAttribute == null)
                    continue;

                var options = GetOptions(propertyInfo, activityPropertyAttribute, scope);

                yield return new ActivityPropertyDescriptor
                (
                    (activityPropertyAttribute.Name ?? propertyInfo.Name).Pascalize(),
                    (activityPropertyAttribute.UIHint ?? InferPropertyUIHint(propertyInfo)),
                    activityPropertyAttribute.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityPropertyAttribute.Hint,
                    options
                );
            }
        }

        private object? GetOptions(PropertyInfo activityProperty, ActivityPropertyAttribute activityPropertyAttribute, IServiceScope scope)
        {
            if (activityPropertyAttribute.OptionsProvider == null)
                return activityPropertyAttribute.Options;

            var providerType = activityPropertyAttribute.OptionsProvider;
            var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetOptions(activityProperty);
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