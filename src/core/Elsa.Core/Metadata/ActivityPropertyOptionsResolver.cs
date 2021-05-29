using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper.Internal;
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
            var activityPropertyAttribute = activityPropertyInfo.GetCustomAttribute<ActivityInputAttribute>();

            if (activityPropertyAttribute == null)
                return null;

            if (activityPropertyAttribute.OptionsProvider == null)
                return activityPropertyAttribute.Options ?? (TryGetEnumOptions(activityPropertyInfo, out var items) ? items : null);

            var providerType = activityPropertyAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetOptions(activityPropertyInfo);
        }

        private bool TryGetEnumOptions(PropertyInfo activityPropertyInfo, out IList<SelectListItem>? items)
        {
            var isNullable = activityPropertyInfo.PropertyType.IsNullableType();
            var propertyType = isNullable ? activityPropertyInfo.PropertyType.GetTypeOfNullable() : activityPropertyInfo.PropertyType;

            items = null;

            if (!propertyType.IsEnum)
                return false;
            
            items = propertyType.GetEnumNames().Select(x => new SelectListItem(x.Humanize(LetterCasing.Title), x)).ToList();

            if (isNullable)
                items.Insert(0, new SelectListItem("-", ""));

            return true;
        }
    }
}