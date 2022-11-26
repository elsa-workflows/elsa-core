using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Elsa.Common.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Humanizer;
using IContainer = Elsa.Workflows.Core.Services.IContainer;

namespace Elsa.Workflows.Management.Implementations;

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
        var activityAttr = activityType.GetCustomAttribute<ActivityAttribute>();
        var ns = activityAttr?.Namespace ?? ActivityTypeNameHelper.GenerateNamespace(activityType);
        var typeName = activityAttr?.Type ?? activityType.Name;
        var typeVersion = activityAttr?.Version ?? 1;
        var fullTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);
        var displayNameAttr = activityType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title);
        var categoryAttr = activityType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? ActivityTypeNameHelper.GetCategoryFromNamespace(ns) ?? "Miscellaneous";
        var descriptionAttr = activityType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description;

        var embeddedPorts =
            from prop in activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let portAttr = prop.GetCustomAttribute<PortAttribute>()
            let portBrowsableAttr = prop.GetCustomAttribute<BrowsableAttribute>()
            where portAttr != null && (portBrowsableAttr == null || portBrowsableAttr.Browsable)
            select new Port
            {
                Name = portAttr.Name ?? prop.Name,
                DisplayName = portAttr.DisplayName ?? portAttr.Name ?? prop.Name,
                Mode = PortMode.Embedded
            };

        var flowNodeAttr = activityType.GetCustomAttribute<FlowNodeAttribute>();
        var flowPorts = flowNodeAttr?.Outcomes.Select(x => new Port
        {
            Mode = PortMode.Port,
            Name = x,
            DisplayName = x
        }).ToDictionary(x => x.Name) ?? new Dictionary<string, Port>();

        var allPorts = embeddedPorts.Concat(flowPorts.Values);
        var properties = activityType.GetProperties();
        var inputProperties = properties.Where(x => typeof(Input).IsAssignableFrom(x.PropertyType) || x.GetCustomAttribute<InputAttribute>() != null).ToList();
        var outputProperties = properties.Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).DistinctBy(x => x.Name).ToList();
        var isTrigger = activityType.IsAssignableTo(typeof(ITrigger));
        var browsableAttr = activityType.GetCustomAttribute<BrowsableAttribute>();

        var descriptor = new ActivityDescriptor
        {
            Category = category,
            Description = description,
            TypeName = fullTypeName,
            Version = typeVersion,
            DisplayName = displayName,
            Kind = isTrigger ? ActivityKind.Trigger : activityAttr?.Kind ?? ActivityKind.Action,
            Ports = allPorts.ToList(),
            Inputs = DescribeInputProperties(inputProperties).ToList(),
            Outputs = DescribeOutputProperties(outputProperties).ToList(),
            IsContainer = typeof(IContainer).IsAssignableFrom(activityType),
            IsBrowsable = browsableAttr == null || browsableAttr.Browsable,
            ActivityType = activityType,
            Constructor = context =>
            {
                var activity = _activityFactory.Create(activityType, context);
                activity.Type = fullTypeName;
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
            var propertyType = propertyInfo.PropertyType;
            var isWrappedProperty = typeof(Input).IsAssignableFrom(propertyType);
            var wrappedPropertyType = !isWrappedProperty ? propertyType : propertyInfo.PropertyType.GenericTypeArguments[0];

            yield return new InputDescriptor
            (
                inputAttribute?.Name ?? propertyInfo.Name,
                wrappedPropertyType,
                isWrappedProperty,
                GetUIHint(wrappedPropertyType, inputAttribute),
                inputAttribute?.DisplayName ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                descriptionAttribute?.Description ?? inputAttribute?.Description,
                _optionsResolver.GetOptions(propertyInfo),
                inputAttribute?.Category,
                inputAttribute?.Order ?? 0,
                _defaultValueResolver.GetDefaultValue(propertyInfo),
                inputAttribute?.DefaultSyntax,
                //inputAttribute?.SupportedSyntaxes, TODO: Come up with a different way to specify support languages for activity inputs. By default, maybe all props should support all registered scripting languages?
                inputAttribute?.IsReadOnly ?? false,
                inputAttribute?.IsBrowsable ?? true
            );
        }
    }

    private IEnumerable<OutputDescriptor> DescribeOutputProperties(IEnumerable<PropertyInfo> properties)
    {
        foreach (var propertyInfo in properties)
        {
            var outputAttribute = propertyInfo.GetCustomAttribute<OutputAttribute>();
            var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            var typeArgs = propertyInfo.PropertyType.GenericTypeArguments;
            var wrappedPropertyType = typeArgs.Any() ? typeArgs[0] : typeof(object);

            yield return new OutputDescriptor
            (
                (outputAttribute?.Name ?? propertyInfo.Name).Pascalize(),
                outputAttribute?.DisplayName ?? propertyInfo.Name.Humanize(LetterCasing.Title),
                wrappedPropertyType,
                descriptionAttribute?.Description ?? outputAttribute?.Description,
                outputAttribute?.IsBrowsable ?? true
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
        
        if (wrappedPropertyType == typeof(Variable))
            return InputUIHints.VariablePicker;

        return InputUIHints.SingleLine;
    }
}