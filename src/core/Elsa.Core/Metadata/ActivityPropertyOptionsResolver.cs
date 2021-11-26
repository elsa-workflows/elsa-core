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
            {
                var options = activityPropertyAttribute.Options;
                
                if (options is IEnumerable<string> enumerable)
                    return new SelectList
                    {
                        Items = enumerable.Select(x => new SelectListItem(x)).ToList()
                    };

                if (TryGetEnumOptions(activityPropertyInfo, out var items))
                    return items;

                return activityPropertyAttribute.Options;
            }

            var providerType = activityPropertyAttribute.OptionsProvider;

            using var scope = _serviceProvider.CreateScope();
            var provider = (IActivityPropertyOptionsProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            return provider.GetOptions(activityPropertyInfo);
        }

        private bool TryGetEnumOptions(PropertyInfo activityPropertyInfo, out SelectList? selectList)
        {
            var isNullable = activityPropertyInfo.PropertyType.IsNullableType();
            var propertyType = isNullable ? activityPropertyInfo.PropertyType.GetTypeOfNullable() : activityPropertyInfo.PropertyType;

            selectList = null;

            if (!propertyType.IsEnum)
                return false;

            var isFlagsEnum = propertyType.GetCustomAttribute<FlagsAttribute>() != null;

            if (isFlagsEnum)
            {
                var items = propertyType.GetEnumNames().Select(x => new SelectListItem(x.Humanize(LetterCasing.Title), ((int)Enum.Parse(propertyType, x)).ToString())).ToList();

                selectList = new SelectList
                {
                    Items = items,
                    IsFlagsEnum = isFlagsEnum
                };
            }
            else
            {
                var items = propertyType.GetEnumNames().Select(x => new SelectListItem(x.Humanize(LetterCasing.Title), x)).ToList();

                if (isNullable)
                    items.Insert(0, new SelectListItem("-", ""));

                selectList = new SelectList
                {
                    Items = items,
                    IsFlagsEnum = isFlagsEnum
                };
            }

            return true;
        }
    }
}