using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Metadata
{
    public class TypedActivityTypeDescriber : IDescribesActivityType
    {
        private readonly IActivityPropertyOptionsResolver _optionsResolver;
        private readonly IActivityPropertyUIHintResolver _uiHintResolver;
        private readonly IActivityPropertyDefaultValueResolver _defaultValueResolver;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TypedActivityTypeDescriber(IActivityPropertyOptionsResolver optionsResolver, IActivityPropertyUIHintResolver uiHintResolver, IActivityPropertyDefaultValueResolver defaultValueResolver, IServiceScopeFactory serviceScopeFactory)
        {
            _optionsResolver = optionsResolver;
            _uiHintResolver = uiHintResolver;
            _defaultValueResolver = defaultValueResolver;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<ActivityDescriptor?> DescribeAsync(Type activityType, CancellationToken cancellationToken = default)
        {
            var activityAttribute = activityType.GetCustomAttribute<ActivityAttribute>(false);
            var typeName = activityAttribute?.Type ?? activityType.Name;
            var displayName = activityAttribute?.DisplayName ?? activityType.Name.Humanize(LetterCasing.Title);
            var description = activityAttribute?.Description;
            var category = activityAttribute?.Category ?? "Miscellaneous";
            var traits = activityAttribute?.Traits ?? ActivityTraits.Action;
            var outcomes = await GetOutcomesAsync(activityAttribute, cancellationToken);
            var properties = activityType.GetProperties();
            var inputProperties = DescribeInputProperties(properties).OrderBy(x => x.Order);
            var outputProperties = DescribeOutputProperties(properties);

            return new ActivityDescriptor
            {
                Type = typeName.Pascalize(),
                DisplayName = displayName,
                Description = description,
                Category = category,
                Traits = traits,
                InputProperties = inputProperties.ToArray(),
                OutputProperties = outputProperties.ToArray(),
                Outcomes = outcomes,
            };
        }

        private async Task<string[]> GetOutcomesAsync(ActivityAttribute? activityAttribute, CancellationToken cancellationToken)
        {
            var outcomesObj = activityAttribute?.Outcomes;
            
            if (outcomesObj == null)
                return new[] { OutcomeNames.Done };

            var outcomesType = outcomesObj.GetType();

            if (outcomesType.IsArray)
                return (string[]) outcomesObj;

            if (outcomesObj is Type providerType && typeof(IOutcomesProvider).IsAssignableFrom(providerType))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var provider = (IOutcomesProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
                var providedOutcomes = await provider.GetOutcomesAsync(cancellationToken);
                return providedOutcomes.ToArray();
            }

            throw new NotSupportedException("The specified outcomes type is not supported. Only string[] and typeof(IOutcomesProvider) are supported.");
        }

        private IEnumerable<ActivityInputDescriptor> DescribeInputProperties(IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                var activityPropertyAttribute = propertyInfo.GetCustomAttribute<ActivityInputAttribute>();

                if (activityPropertyAttribute == null)
                    continue;

                yield return new ActivityInputDescriptor
                (
                    (activityPropertyAttribute.Name ?? propertyInfo.Name).Pascalize(),
                    propertyInfo.PropertyType,
                    _uiHintResolver.GetUIHint(propertyInfo),
                    activityPropertyAttribute.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityPropertyAttribute.Hint,
                    _optionsResolver.GetOptions(propertyInfo),
                    activityPropertyAttribute.Category,
                    activityPropertyAttribute.Order,
                    _defaultValueResolver.GetDefaultValue(propertyInfo),
                    activityPropertyAttribute.DefaultSyntax,
                    activityPropertyAttribute.SupportedSyntaxes
                );
            }
        }

        private IEnumerable<ActivityOutputDescriptor> DescribeOutputProperties(IEnumerable<PropertyInfo> properties)
        {
            foreach (var propertyInfo in properties)
            {
                var activityPropertyAttribute = propertyInfo.GetCustomAttribute<ActivityOutputAttribute>();

                if (activityPropertyAttribute == null)
                    continue;

                yield return new ActivityOutputDescriptor
                (
                    (activityPropertyAttribute.Name ?? propertyInfo.Name).Pascalize(),
                    propertyInfo.PropertyType,
                    activityPropertyAttribute.Hint
                );
            }
        }
    }
}