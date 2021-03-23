using System;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Design;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Metadata
{
    public class ActivityPropertyOptionsResolver : IActivityPropertyOptionsResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ActivityPropertyOptionsResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? GetOptions(PropertyInfo activityPropertyInfo)
        {
            var activityPropertyAttribute = activityPropertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

            if (activityPropertyAttribute == null)
                return null;

            if (activityPropertyAttribute.OptionsProvider == null)
            {
                if (activityPropertyInfo.PropertyType.IsEnum)
                    return activityPropertyInfo.PropertyType.GetEnumNames().Select(x => new SelectListItem(x.Humanize(LetterCasing.Title), x));

                return activityPropertyAttribute.Options;
            }

            var providerType = activityPropertyAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetOptions(activityPropertyInfo);
        }
    }
}