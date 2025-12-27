using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Management.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Default parameter binding implementation for host method activities.
/// </summary>
public class DefaultHostMethodParameterValueProvider : IHostMethodParameterValueProvider
{
    public ValueTask<HostMethodParameterValueProviderResult> GetValueAsync(HostMethodParameterValueProviderContext context)
    {
        var parameter = context.Parameter;

        // Provided by runtime.
        if (parameter.ParameterType == typeof(CancellationToken))
            return ValueTask.FromResult(HostMethodParameterValueProviderResult.HandledValue(context.CancellationToken));

        if (parameter.ParameterType == typeof(ActivityExecutionContext))
            return ValueTask.FromResult(HostMethodParameterValueProviderResult.HandledValue(context.ActivityExecutionContext));

        // Resolve from DI if explicitly requested.
        if (parameter.GetCustomAttribute<FromServicesAttribute>() != null)
        {
            var service = context.ServiceProvider.GetService(parameter.ParameterType);
            return ValueTask.FromResult(HostMethodParameterValueProviderResult.HandledValue(service));
        }

        // Resolve from workflow inputs (default).
        var inputDescriptor = context.InputDescriptors.FirstOrDefault(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
        if (inputDescriptor == null)
            return ValueTask.FromResult(HostMethodParameterValueProviderResult.Unhandled);

        var input = (Input?)inputDescriptor.ValueGetter(context.Activity);
        var inputValue = input != null ? context.ActivityExecutionContext.Get(input.MemoryBlockReference()) : null;

        if (inputValue is System.Dynamic.ExpandoObject expandoObject)
            inputValue = expandoObject.ConvertTo(parameter.ParameterType);

        return ValueTask.FromResult(HostMethodParameterValueProviderResult.HandledValue(inputValue));
    }
}
