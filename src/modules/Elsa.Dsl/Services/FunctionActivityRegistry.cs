using System.Text.Json;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Interpreters;
using Elsa.Dsl.Models;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Services;

/// <inheritdoc />
public class FunctionActivityRegistry : IFunctionActivityRegistry
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IDictionary<string, FunctionActivityDescriptor> _dictionary = new Dictionary<string, FunctionActivityDescriptor>();

    /// <summary>
    /// Creates a new instance of the <see cref="FunctionActivityRegistry"/> class.
    /// </summary>
    public FunctionActivityRegistry(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }

    /// <inheritdoc />
    public void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default, Action<IActivity>? configure = default)
    {
        var descriptor = new FunctionActivityDescriptor(functionName, activityTypeName, propertyNames, configure);
        RegisterFunction(descriptor);
    }

    /// <inheritdoc />
    public void RegisterFunction(FunctionActivityDescriptor descriptor)
    {
        _dictionary.Add(descriptor.FunctionName, descriptor);
    }

    /// <inheritdoc />
    public IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default)
    {
        if (!_dictionary.TryGetValue(functionName, out var descriptor))
            throw new Exception($"Could not resolve function {functionName}. Did you forget to register it?");

        var activityDescriptor = _activityRegistry.Find(x => x.Name == descriptor.ActivityTypeName || x.TypeName == descriptor.ActivityTypeName);

        if (activityDescriptor == null)
            throw new Exception($"Could not find activity descriptor for activity type {descriptor.ActivityTypeName}");

        var propertyNameList = descriptor.PropertyNames?.ToList() ?? new List<string>();
        var propertyDescriptors = activityDescriptor.Inputs.Cast<PropertyDescriptor>().Concat(activityDescriptor.Outputs).ToList();

        var properties = propertyNameList
            .Select(propertyName => propertyDescriptors.FirstOrDefault(x => x.Name == propertyName))
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var dummyJsonElement = JsonDocument.Parse("{}").RootElement;
        var constructorContext = new ActivityConstructorContext(activityDescriptor, dummyJsonElement, new JsonSerializerOptions());
        var activity = activityDescriptor.Constructor(constructorContext);

        // Apply each argument in order of the described properties.
        var index = 0;

        if (arguments != null)
            foreach (var argument in arguments)
            {
                var property = properties[index++];
                SetPropertyValue(activity, property, argument);
            }

        descriptor.Configure?.Invoke(activity);
        return activity;
    }

    private void SetPropertyValue(IActivity target, PropertyDescriptor propertyDescriptor, object? value)
    {
        value = propertyDescriptor switch
        {
            InputDescriptor inputDescriptor => CreateInputValue(inputDescriptor, value),
            OutputDescriptor outputDescriptor => CreateOutputValue(outputDescriptor, value),
            _ => value
        };

        propertyDescriptor.ValueSetter(target, value);
    }

    private Input CreateInputValue(InputDescriptor inputDescriptor, object? propertyValue)
    {
        if (propertyValue is Input input)
            return input;

        var underlyingType = inputDescriptor.Type;
        var parsedPropertyValue = propertyValue.ConvertTo(underlyingType);
        var propertyValueType = parsedPropertyValue?.GetType();
        var inputType = typeof(Input<>).MakeGenericType(underlyingType);

        if (parsedPropertyValue is ExternalExpressionReference externalExpressionReference)
            return (Input)Activator.CreateInstance(inputType, externalExpressionReference.Expression, externalExpressionReference.BlockReference)!;

        if (propertyValueType != null)
        {
            // Create a literal value.
            var literalType = typeof(Literal<>).MakeGenericType(underlyingType);
            var hasCtorWithSpecifiedType = inputType.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType.IsAssignableFrom(literalType)));

            if (hasCtorWithSpecifiedType)
            {
                var literalValue = Activator.CreateInstance(literalType, parsedPropertyValue)!;
                return (Input)Activator.CreateInstance(inputType, literalValue)!;
            }
        }

        return (Input)Activator.CreateInstance(inputType, parsedPropertyValue)!;
    }

    private Output CreateOutputValue(OutputDescriptor outputDescriptor, object? propertyValue)
    {
        if (propertyValue is Output output)
            return output;

        var underlyingType = outputDescriptor.Type;
        var propertyValueType = propertyValue?.GetType();
        var outputType = typeof(Output<>).MakeGenericType(underlyingType);

        if (propertyValueType != null)
        {
            var hasCtorWithSpecifiedType = outputType.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType.IsAssignableFrom(propertyValueType)));

            if (hasCtorWithSpecifiedType)
                return (Output)Activator.CreateInstance(outputType, propertyValue, null)!;
        }

        var convertedValue = propertyValue.ConvertTo(underlyingType);

        return (Output)Activator.CreateInstance(outputType, convertedValue)!;
    }
}