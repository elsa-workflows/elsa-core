using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Humanizer;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityDescriber : IActivityDescriber
{
    private readonly IPropertyOptionsResolver _optionsResolver;
    private readonly IPropertyDefaultValueResolver _defaultValueResolver;
    private readonly IActivityFactory _activityFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityDescriber(IPropertyOptionsResolver optionsResolver, IPropertyDefaultValueResolver defaultValueResolver, IActivityFactory activityFactory)
    {
        _optionsResolver = optionsResolver;
        _defaultValueResolver = defaultValueResolver;
        _activityFactory = activityFactory;
    }

    /// <inheritdoc />
    public async Task<ActivityDescriptor> DescribeActivityAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default)
    {
        var activityAttr = activityType.GetCustomAttribute<ActivityAttribute>();
        var ns = activityAttr?.Namespace ?? ActivityTypeNameHelper.GenerateNamespace(activityType) ?? "Elsa";
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
            select new Port
            {
                Name = portAttr?.Name ?? prop.Name,
                DisplayName = portAttr?.DisplayName ?? portAttr?.Name ?? prop.Name,
                Type = PortType.Embedded,
                IsBrowsable = portAttr != null && (portBrowsableAttr == null || portBrowsableAttr.Browsable)
            };

        var flowNodeAttr = activityType.GetCustomAttribute<FlowNodeAttribute>();
        var flowPorts = flowNodeAttr?.Outcomes.Select(x => new Port
        {
            Type = PortType.Flow,
            Name = x,
            DisplayName = x
        }).ToDictionary(x => x.Name) ?? new Dictionary<string, Port>();
        
        var allPorts = embeddedPorts.Concat(flowPorts.Values);
        var inputProperties = GetInputProperties(activityType).ToList();
        var outputProperties = GetOutputProperties(activityType).ToList();
        var isTrigger = activityType.IsAssignableTo(typeof(ITrigger));
        var browsableAttr = activityType.GetCustomAttribute<BrowsableAttribute>();

        var descriptor = new ActivityDescriptor
        {
            TypeName = fullTypeName,
            Namespace = ns,
            Name = typeName,
            Category = category,
            Description = description,
            Version = typeVersion,
            DisplayName = displayName,
            Kind = isTrigger ? ActivityKind.Trigger : activityAttr?.Kind ?? ActivityKind.Action,
            Ports = allPorts.ToList(),
            Inputs = (await DescribeInputPropertiesAsync(inputProperties, cancellationToken)).ToList(),
            Outputs = (await DescribeOutputPropertiesAsync(outputProperties, cancellationToken)).ToList(),
            IsContainer = typeof(IContainer).IsAssignableFrom(activityType),
            IsBrowsable = browsableAttr == null || browsableAttr.Browsable,
            Constructor = context =>
            {
                var activity = _activityFactory.Create(activityType, context);
                activity.Type = fullTypeName;
                return activity;
            }
        };
        
        return descriptor;
    }

    /// <inheritdoc />
    public IEnumerable<PropertyInfo> GetInputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType) => 
        activityType.GetProperties().Where(x => typeof(Input).IsAssignableFrom(x.PropertyType) || x.GetCustomAttribute<InputAttribute>() != null).DistinctBy(x => x.Name);

    /// <inheritdoc />
    public IEnumerable<PropertyInfo> GetOutputProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType) => 
        activityType.GetProperties().Where(x => typeof(Output).IsAssignableFrom(x.PropertyType)).DistinctBy(x => x.Name).ToList();

    /// <inheritdoc />
    public Task<OutputDescriptor> DescribeOutputPropertyAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        var outputAttribute = propertyInfo.GetCustomAttribute<OutputAttribute>();
        var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        var typeArgs = propertyInfo.PropertyType.GenericTypeArguments;
        var wrappedPropertyType = typeArgs.Any() ? typeArgs[0] : typeof(object);

        return Task.FromResult(new OutputDescriptor
        (
            (outputAttribute?.Name ?? propertyInfo.Name).Pascalize(),
            outputAttribute?.DisplayName ?? propertyInfo.Name.Humanize(LetterCasing.Title),
            wrappedPropertyType,
            propertyInfo.GetValue,
            descriptionAttribute?.Description ?? outputAttribute?.Description,
            outputAttribute?.IsBrowsable ?? true
        ));
    }

    /// <inheritdoc />
    public async Task<InputDescriptor> DescribeInputPropertyAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        var propertyType = propertyInfo.PropertyType;
        var isWrappedProperty = typeof(Input).IsAssignableFrom(propertyType);
        var wrappedPropertyType = !isWrappedProperty ? propertyType : propertyInfo.PropertyType.GenericTypeArguments[0];

        if (wrappedPropertyType.IsNullableType())
            wrappedPropertyType = wrappedPropertyType.GetTypeOfNullable();

        var inputOptions = await _optionsResolver.GetOptionsAsync(propertyInfo, cancellationToken);
        
        return new InputDescriptor
        (
            inputAttribute?.Name ?? propertyInfo.Name,
            wrappedPropertyType,
            propertyInfo.GetValue,
            propertyInfo.SetValue,
            isWrappedProperty,
            GetUIHint(wrappedPropertyType, inputAttribute),
            inputAttribute?.DisplayName ?? propertyInfo.Name.Humanize(LetterCasing.Title),
            descriptionAttribute?.Description ?? inputAttribute?.Description,
            inputOptions,
            inputAttribute?.Category,
            inputAttribute?.Order ?? 0,
            _defaultValueResolver.GetDefaultValue(propertyInfo),
            inputAttribute?.DefaultSyntax,
            //inputAttribute?.SupportedSyntaxes, TODO: Come up with a different way to specify support languages for activity inputs. By default, maybe all props should support all registered scripting languages?
            inputAttribute?.IsReadOnly ?? false,
            inputAttribute?.IsBrowsable ?? true
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type activityType, CancellationToken cancellationToken = default)
    {
        var properties = GetInputProperties(activityType);
        return await DescribeInputPropertiesAsync(properties, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OutputDescriptor>> DescribeOutputPropertiesAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type activityType, CancellationToken cancellationToken = default) => 
        await DescribeOutputPropertiesAsync(GetOutputProperties(activityType), cancellationToken);

    private async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await DescribeInputPropertyAsync(x, cancellationToken)));
    }

    private async Task<IEnumerable<OutputDescriptor>> DescribeOutputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await DescribeOutputPropertyAsync(x, cancellationToken)));
    }

    private static string GetUIHint(Type wrappedPropertyType, InputAttribute? inputAttribute)
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

        if (wrappedPropertyType == typeof(Type))
            return InputUIHints.TypePicker;

        return InputUIHints.SingleLine;
    }
}