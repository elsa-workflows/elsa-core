using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Humanizer;

namespace Elsa.Metadata
{
    public class TypedActivityTypeDescriber : IDescribesActivityType
    {
        private readonly IActivityPropertyOptionsResolver _optionsResolver;
        private readonly IActivityPropertyUIHintResolver _uiHintResolver;
        private readonly IActivityPropertyDefaultValueResolver _defaultValueResolver;

        public TypedActivityTypeDescriber(IActivityPropertyOptionsResolver optionsResolver, IActivityPropertyUIHintResolver uiHintResolver, IActivityPropertyDefaultValueResolver defaultValueResolver)
        {
            _optionsResolver = optionsResolver;
            _uiHintResolver = uiHintResolver;
            _defaultValueResolver = defaultValueResolver;
        }

        public ActivityDescriptor? Describe(Type activityType)
        {
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
            };
        }

        private IEnumerable<ActivityPropertyDescriptor> DescribeProperties(Type activityType)
        {
            var properties = activityType.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var activityPropertyAttribute = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityPropertyAttribute == null)
                    continue;

                yield return new ActivityPropertyDescriptor
                (
                    (activityPropertyAttribute.Name ?? propertyInfo.Name).Pascalize(),
                    propertyInfo.PropertyType,
                    _uiHintResolver.GetUIHint(propertyInfo),
                    activityPropertyAttribute.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityPropertyAttribute.Hint,
                    _optionsResolver.GetOptions(propertyInfo),
                    activityPropertyAttribute.Category,
                    _defaultValueResolver.GetDefaultValue(propertyInfo),
                    activityPropertyAttribute.DefaultSyntax,
                    activityPropertyAttribute.SupportedSyntaxes
                );
            }
        }
    }
}