using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Humanizer;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityDescriber : IActivityDescriber
{
    //private readonly IPropertyOptionsResolver _optionsResolver;
    private readonly IPropertyDefaultValueResolver _defaultValueResolver;
    private readonly IActivityFactory _activityFactory;
    private readonly IPropertyUIHandlerResolver _propertyUIHandlerResolver;
    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityDescriber(IPropertyDefaultValueResolver defaultValueResolver, IActivityFactory activityFactory, IPropertyUIHandlerResolver propertyUIHandlerResolver)
    {
        //_optionsResolver = optionsResolver;
        _defaultValueResolver = defaultValueResolver;
        _activityFactory = activityFactory;
        _propertyUIHandlerResolver = propertyUIHandlerResolver;
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
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType)
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
        var isTerminal = activityType.FindInterfaces((type, criteria) => type == typeof(ITerminalNode), null).Any();
        var isStart = activityType.FindInterfaces((type, criteria) => type == typeof(IStartNode), null).Any();
        var attributes = activityType.GetCustomAttributes(true).Cast<Attribute>().ToList();
        var outputAttribute = attributes.OfType<OutputAttribute>().FirstOrDefault();

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
            IsStart = isStart,
            IsTerminal = isTerminal,
            Attributes = attributes,
            Constructor = context =>
            {
                var activity = _activityFactory.Create(activityType, context);
                activity.Type = fullTypeName;
                return activity;
            }
        };

        // If the activity has a default output, set its IsSerializable property to the value of the OutputAttribute.IsSerializable property.
        var defaultOutputDescriptor = descriptor.Outputs.FirstOrDefault(x => x.Name == ActivityOutputRegister.DefaultOutputName);

        if (defaultOutputDescriptor != null)
        {
            var isResultSerializable = outputAttribute?.IsSerializable;
            defaultOutputDescriptor.IsSerializable = isResultSerializable;
        }

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
            propertyInfo.SetValue,
            propertyInfo,
            descriptionAttribute?.Description ?? outputAttribute?.Description,
            outputAttribute?.IsBrowsable ?? true,
            outputAttribute?.IsSerializable
        ));
    }

    /// <inheritdoc />
    public async Task<InputDescriptor> DescribeInputPropertyAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var descriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        var propertyType = propertyInfo.PropertyType;
        var isWrappedProperty = typeof(Input).IsAssignableFrom(propertyType);
        var autoEvaluate = inputAttribute?.AutoEvaluate ?? true;
        var wrappedPropertyType = !isWrappedProperty ? propertyType : propertyInfo.PropertyType.GenericTypeArguments[0];

        if (wrappedPropertyType.IsNullableType())
            wrappedPropertyType = wrappedPropertyType.GetTypeOfNullable();
        
        var uiSpecification = await _propertyUIHandlerResolver.GetUIPropertiesAsync(propertyInfo, null, cancellationToken);

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
            inputAttribute?.Category,
            inputAttribute?.Order ?? 0,
            _defaultValueResolver.GetDefaultValue(propertyInfo),
            inputAttribute?.DefaultSyntax,
            inputAttribute?.IsReadOnly ?? false,
            inputAttribute?.IsBrowsable ?? true,
            inputAttribute?.IsSerializable ?? true,
            false,
            autoEvaluate,
            default,
            propertyInfo,
            uiSpecification
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default)
    {
        var properties = GetInputProperties(activityType);
        return await DescribeInputPropertiesAsync(properties, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OutputDescriptor>> DescribeOutputPropertiesAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default)
    {
        return await DescribeOutputPropertiesAsync(GetOutputProperties(activityType), cancellationToken);
    }
    
    public static string GetUIHint(Type wrappedPropertyType, InputAttribute? inputAttribute = null)
    {
        if (inputAttribute?.UIHint != null)
            return inputAttribute.UIHint;

        if (wrappedPropertyType == typeof(bool) || wrappedPropertyType == typeof(bool?))
            return InputUIHints.Checkbox;

        if (wrappedPropertyType == typeof(string))
            return InputUIHints.SingleLine;

        if (typeof(IEnumerable).IsAssignableFrom(wrappedPropertyType))
            return InputUIHints.DropDown;

        if (wrappedPropertyType.IsEnum || wrappedPropertyType.IsNullableType() && wrappedPropertyType.GetTypeOfNullable().IsEnum)
            return InputUIHints.DropDown;

        if (wrappedPropertyType == typeof(Variable))
            return InputUIHints.VariablePicker;

        if (wrappedPropertyType == typeof(Type))
            return InputUIHints.TypePicker;

        return InputUIHints.SingleLine;
    }

    private async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await DescribeInputPropertyAsync(x, cancellationToken)));
    }

    private async Task<IEnumerable<OutputDescriptor>> DescribeOutputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await DescribeOutputPropertyAsync(x, cancellationToken)));
    }
}