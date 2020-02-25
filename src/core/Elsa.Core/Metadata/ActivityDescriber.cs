using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Humanizer;
using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public static class ActivityDescriber
    {
        public static ActivityDescriptor Describe<T>() where T : IActivity
        {
            return Describe(typeof(T));
        }

        public static ActivityDescriptor Describe(Type activityType)
        {
            var activityDefinitionAttribute = activityType.GetCustomAttribute<ActivityDefinitionAttribute>();
            var typeName = activityDefinitionAttribute?.Type ?? activityType.Name;

            var displayName =
                activityDefinitionAttribute?.DisplayName ??
                activityType.Name.Humanize(LetterCasing.Title);

            var description = activityDefinitionAttribute?.Description;
            var runtimeDescription = activityDefinitionAttribute?.RuntimeDescription;
            var category = activityDefinitionAttribute?.Category ?? "Miscellaneous";
            var icon = activityDefinitionAttribute?.Icon;
            var outcomes = activityDefinitionAttribute?.Outcomes ?? new[] { OutcomeNames.Done };
            var properties = DescribeProperties(activityType);

            return new ActivityDescriptor
            {
                Type = typeName.Pascalize(),
                DisplayName = displayName,
                Description = description,
                RuntimeDescription = runtimeDescription,
                Category = category,
                Icon = icon,
                Properties = properties.ToArray(),
                Outcomes = outcomes
            };
        }

        private static IEnumerable<ActivityPropertyDescriptor> DescribeProperties(Type activityType)
        {
            var properties = activityType.GetProperties();

            foreach (var propertyInfo in properties)
            {
                var activityProperty = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

                if (activityProperty == null)
                    continue;

                yield return new ActivityPropertyDescriptor
                (
                    (activityProperty.Name ?? propertyInfo.Name).Camelize(),
                    (activityProperty.Type ?? DeterminePropertyType(propertyInfo)).Camelize(),
                    activityProperty.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                    activityProperty.Hint,
                    GetPropertyTypeOptions(propertyInfo)
                );
            }
        }

        private static object GetPropertyTypeOptions(PropertyInfo propertyInfo)
        {
            var optionsAttribute = propertyInfo.GetCustomAttribute<ActivityPropertyOptionsAttribute>();

            return optionsAttribute?.GetOptions() ?? new object();
        }

        private static string DeterminePropertyType(PropertyInfo propertyInfo)
        {
            var type = propertyInfo.PropertyType;

            // if (typeof(IWorkflowScriptExpression).IsAssignableFrom(type))
            //     return ActivityPropertyTypes.Expression;

            if (type == typeof(bool) || type == typeof(bool?))
                return ActivityPropertyTypes.Boolean;

            if (type == typeof(string))
                return ActivityPropertyTypes.Text;

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return ActivityPropertyTypes.List;

            return ActivityPropertyTypes.Text;
        }
    }

    //public class ActivityDescriber : IActivityDescriber
    //{
    //    private readonly IEnumerable<IActivityPropertyOptionsProvider> optionsProviders;

    //    public ActivityDescriber(IEnumerable<IActivityPropertyOptionsProvider> optionsProviders)
    //    {
    //        this.optionsProviders = optionsProviders;
    //    }

    //    public ActivityDescriptor Describe(Type activityType)
    //    {
    //        var activityDefinitionAttribute = activityType.GetCustomAttribute<ActivityDefinitionAttribute>();
    //        var typeName = activityDefinitionAttribute?.Type ?? activityType.Name;

    //        var displayName =
    //            activityDefinitionAttribute?.DisplayName ??
    //            activityType.Name.Humanize(LetterCasing.Title);

    //        var description = activityDefinitionAttribute?.Description;
    //        var runtimeDescription = activityDefinitionAttribute?.RuntimeDescription;
    //        var category = activityDefinitionAttribute?.Category ?? "Miscellaneous";
    //        var icon = activityDefinitionAttribute?.Icon;
    //        var outcomes = activityDefinitionAttribute?.Outcomes ?? new[] { OutcomeNames.Done };
    //        var properties = DescribeProperties(activityType);

    //        return new ActivityDescriptor
    //        {
    //            Type = typeName.Pascalize(),
    //            DisplayName = displayName,
    //            Description = description,
    //            RuntimeDescription = runtimeDescription,
    //            Category = category,
    //            Icon = icon,
    //            Properties = properties.ToArray(),
    //            Outcomes = outcomes
    //        };
    //    }

    //    private IEnumerable<ActivityPropertyDescriptor> DescribeProperties(Type activityType)
    //    {
    //        var properties = activityType.GetProperties();

    //        foreach (var propertyInfo in properties)
    //        {
    //            var activityProperty = propertyInfo.GetCustomAttribute<ActivityPropertyAttribute>();

    //            if (activityProperty == null)
    //                continue;

    //            yield return new ActivityPropertyDescriptor
    //            (
    //                (activityProperty.Name ?? propertyInfo.Name).Camelize(),
    //                (activityProperty.Type ?? DeterminePropertyType(propertyInfo)).Pascalize(),
    //                activityProperty.Label ?? propertyInfo.Name.Humanize(LetterCasing.Title),
    //                activityProperty.Hint,
    //                GetPropertyTypeOptions(propertyInfo)
    //            );
    //        }
    //    }

    //    private JObject GetPropertyTypeOptions(PropertyInfo propertyInfo)
    //    {
    //        var options = new JObject();

    //        foreach (var provider in optionsProviders.Where(x => x.SupportsProperty(propertyInfo))) 
    //            provider.SupplyOptions(propertyInfo, options);

    //        return options;
    //    }

    //    private string DeterminePropertyType(PropertyInfo propertyInfo)
    //    {
    //        var type = propertyInfo.PropertyType;

    //        if (typeof(IWorkflowExpression).IsAssignableFrom(type))
    //            return ActivityPropertyTypes.Expression;

    //        if (type == typeof(bool) || type == typeof(bool?))
    //            return ActivityPropertyTypes.Boolean;

    //        if (type == typeof(string))
    //            return ActivityPropertyTypes.Text;

    //        if (typeof(IEnumerable).IsAssignableFrom(type))
    //            return ActivityPropertyTypes.List;

    //        return ActivityPropertyTypes.Text;
    //    }
    //}
}