using System;
using System.Reflection;
using Elsa.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Metadata
{
    public class ActivityPropertyDefaultValueResolver : IActivityPropertyDefaultValueResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ActivityPropertyDefaultValueResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? GetDefaultValue(PropertyInfo activityPropertyInfo)
        {
            var activityPropertyAttribute = activityPropertyInfo.GetCustomAttribute<ActivityInputAttribute>();

            if (activityPropertyAttribute == null)
                return null;

            if (activityPropertyAttribute.DefaultValueProvider == null)
                return activityPropertyAttribute.DefaultValue;

            var providerType = activityPropertyAttribute.DefaultValueProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyDefaultValueProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetDefaultValue(activityPropertyInfo);
        }
    }
}