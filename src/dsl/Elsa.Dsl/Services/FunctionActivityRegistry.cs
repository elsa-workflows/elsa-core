using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Contracts;
using Elsa.Dsl.Abstractions;
using Elsa.Dsl.Interpreters;
using Elsa.Helpers;
using Elsa.Models;

namespace Elsa.Dsl.Services;

public class FunctionActivityRegistry : IFunctionActivityRegistry
{
    private readonly ITypeSystem _typeSystem;
    private readonly IDictionary<string, FunctionActivityDescriptor> _dictionary = new Dictionary<string, FunctionActivityDescriptor>();

    public FunctionActivityRegistry(ITypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
    }

    public void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default)
    {
        var typeDescriptor = _typeSystem.ResolveTypeName(activityTypeName);

        if (typeDescriptor == null)
            throw new Exception($"Could not find activity type {activityTypeName}. Did you forget to register it?");


        if (typeDescriptor.Kind != TypeKind.Activity)
            throw new Exception($"Only activity types can be mapped to functions. You are trying to map {typeDescriptor.Type.Name}, which is a different kind: {typeDescriptor.Kind}");

        var activityType = typeDescriptor.Type;
        var propertyNameList = propertyNames?.ToList() ?? new List<string>();
        var properties = propertyNameList.Select(propertyName =>
        {
            var property = activityType.GetProperty(propertyName);

            if (property == null)
                throw new Exception($"Activity type {typeDescriptor.Type.Name} does not have a property named {propertyName}");

            return property;
        }).ToList();

        var descriptor = new FunctionActivityDescriptor(activityType, properties);
        _dictionary.Add(functionName, descriptor);
    }

    public IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default)
    {
        if (!_dictionary.TryGetValue(functionName, out var descriptor))
            throw new Exception($"Could not resolve function {functionName}. Did you forget to register it?");

        var activityType = descriptor.ActivityType;
        var activity = (IActivity)Activator.CreateInstance(activityType)!;

        // Apply each argument in order of the described properties.
        var index = 0;
        var properties = descriptor.Properties.ToList();

        if (arguments != null)
            foreach (var argument in arguments)
            {
                var property = properties[index++];
                SetPropertyValue(activity, property, argument);
            }

        return activity;
    }
        
    private void SetPropertyValue(object target, PropertyInfo propertyInfo, object? value)
    {
        if (typeof(Input).IsAssignableFrom(propertyInfo.PropertyType))
            value = CreateInputValue(propertyInfo, value);
        else if (typeof(Output).IsAssignableFrom(propertyInfo.PropertyType))
            value = CreateOutputValue(propertyInfo, value);

        propertyInfo.SetValue(target, value, null);
    }

    private Input CreateInputValue(PropertyInfo propertyInfo, object? propertyValue)
    {
        if (propertyValue is Input input)
            return input;
            
        var underlyingType = propertyInfo.PropertyType.GetGenericArguments().First();
        var propertyValueType = propertyValue?.GetType();
        var inputType = typeof(Input<>).MakeGenericType(underlyingType);

        if (propertyValue is ExternalExpressionReference externalExpressionReference)
            return (Input)Activator.CreateInstance(inputType, externalExpressionReference.Expression, externalExpressionReference.Reference)!;

        if (propertyValueType != null)
        {
            var hasCtorWithSpecifiedType = inputType.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType.IsAssignableFrom(propertyValueType)));

            if (hasCtorWithSpecifiedType)
                return (Input)Activator.CreateInstance(inputType, propertyValue)!;
        }

        var convertedValue = propertyValue.ConvertTo(underlyingType);

        return (Input)Activator.CreateInstance(inputType, convertedValue)!;
    }
        
    private Output CreateOutputValue(PropertyInfo propertyInfo, object? propertyValue)
    {
        if (propertyValue is Output output)
            return output;
            
        var underlyingType = propertyInfo.PropertyType.GetGenericArguments().First();
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

    private record FunctionActivityDescriptor(Type ActivityType, ICollection<PropertyInfo> Properties);
}