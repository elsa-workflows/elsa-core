using System;
using System.Reflection;
using Elsa.Attributes;
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
                return activityPropertyAttribute.Options;

            var providerType = activityPropertyAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetOptions(activityPropertyInfo);
        }
    }
}