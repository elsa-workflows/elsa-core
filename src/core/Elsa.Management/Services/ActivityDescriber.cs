using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Management.Contracts;
using Elsa.Management.Extensions;
using Elsa.Management.Models;
using Elsa.Models;
using Humanizer;

namespace Elsa.Management.Services;

public class ActivityDescriber : IActivityDescriber
{
    private readonly IPropertyOptionsResolver _optionsResolver;
    private readonly IPropertyDefaultValueResolver _defaultValueResolver;
    private readonly IActivityFactory _activityFactory;

    public ActivityDescriber(IPropertyOptionsResolver optionsResolver, IPropertyDefaultValueResolver defaultValueResolver, IActivityFactory activityFactory)
    {
        _optionsResolver = optionsResolver;
        _defaultValueResolver = defaultValueResolver;
        _activityFactory = activityFactory;
    }

    public ValueTask<ActivityDescriptor> DescribeActivityAsync(Type activityType, CancellationToken cancellationToken = default)
    {
        var ns = TypeNameHelper.GenerateNamespace(activityType);
        var typeName = activityType.Name;
        var fullTypeName = TypeNameHelper.GenerateTypeName(activityType, ns);
        var displayNameAttr = activityType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title);
        var categoryAttr = activityType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? TypeNameHelper.GetCategoryFromNamespace(ns) ?? "Miscellaneous";
        var descriptionAttr = activityType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description;

        var outboundPorts =
            from prop in activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let portAttr = prop.GetCustomAttribute<OutboundAttribute>()
            where portAttr != null
            select new Port
            {
                Name = portAttr.Name ?? prop.Name,
                DisplayName = portAttr.DisplayName ?? portAttr.Name ?? prop.Name
            };

        var properties = activityType.GetProperties();
        var inputProperties = properties.Where(x => typeof(Input).IsAssignableFrom(x.PropertyType)).ToList();
        var outputProperties = properties.Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).ToList();
        var isTrigger = activityType.IsAssignableTo(typeof(ITrigger));

        var descriptor = new ActivityDescriptor
        {
            Category = category,
            Description = description,
            NodeType = fullTypeName,
            DisplayName = displayName,
            Traits = isTrigger ? ActivityTraits.Trigger : ActivityTraits.Action,
            OutPorts = outboundPorts.ToList(),
            InputProperties = DescribeInputProperties(inputProperties).ToList(),
            OutputProperties = DescribeOutputProperties(outputProperties).ToList(),
            Constructor = context =>
            {
                var activity = _activityFactory.Create(activityType, context);
                activity.NodeType = fullTypeName;
                return activity;
            }
        };

        return ValueTask.FromResult(descriptor);
    }
    
    private IEnumerable<InputDescriptor> DescribeInputProperties(IEnumerable<PropertyInfo> properties)
    {
        foreach (var propertyInfo in properties)
        {
            var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
            var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            var wrappedPropertyType = propertyInfo.PropertyType.GenericTypeArguments[0];

            yield return new InputDescriptor
            (
                inputAttribute?.Name ?? propertyInfo.Name,
                wrappedPropertyType,
                GetUIHint(wrappedPropertyType, inputAttribute),
                inputAttribute?.DisplayName ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                descriptionAttribute?.Description,
                _optionsResolver.GetOptions(propertyInfo),
                inputAttribute?.Category,
                inputAttribute?.Order ?? 0,
                _defaultValueResolver.GetDefaultValue(propertyInfo),
                inputAttribute?.DefaultSyntax,
                inputAttribute?.SupportedSyntaxes,
                inputAttribute?.IsReadOnly ?? false,
                inputAttribute?.IsBrowsable ?? true
            );
        }
    }

    private IEnumerable<OutputDescriptor> DescribeOutputProperties(IEnumerable<PropertyInfo> properties)
    {
        foreach (var propertyInfo in properties)
        {
            var activityPropertyAttribute = propertyInfo.GetCustomAttribute<OutputAttribute>();
            var wrappedPropertyType = propertyInfo.PropertyType.GenericTypeArguments[0];

            yield return new OutputDescriptor
            (
                (activityPropertyAttribute?.Name ?? propertyInfo.Name).Pascalize(),
                wrappedPropertyType,
                activityPropertyAttribute?.Description
            );
        }
    }

    private string GetUIHint(Type wrappedPropertyType, InputAttribute? inputAttribute)
    {
        if (inputAttribute?.UIHint != null)
            return inputAttribute.UIHint;

        if (wrappedPropertyType == typeof(bool) || wrappedPropertyType == typeof(bool?))
            return InputUIHints.Checkbox;

        if (wrappedPropertyType == typeof(string))
            return InputUIHints.SingleLine;

        if (typeof(IEnumerable).IsAssignableFrom(wrappedPropertyType))
            return InputUIHints.Dropdown;

        if (wrappedPropertyType.IsEnum || wrappedPropertyType.IsNullableType() && wrappedPropertyType.GetTypeOfNullable().IsEnum)
            return InputUIHints.Dropdown;

        return InputUIHints.SingleLine;
    }
}