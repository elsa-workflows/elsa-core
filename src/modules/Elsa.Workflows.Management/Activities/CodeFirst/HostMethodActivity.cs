using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using System.Text.Json.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Activities.CodeFirst;

/// <summary>
/// Executes a public async method on a configured CLR type. Internal activity used by <see cref="HostMethodActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class HostMethodActivity : CodeActivity
{
    [JsonIgnore] internal Type HostType { get; set; } = null!;
    [JsonIgnore] internal string MethodName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;
        var serviceProvider = context.GetRequiredService<IServiceProvider>();

        var hostInstance = ActivatorUtilities.CreateInstance(serviceProvider, HostType);
        var method = ResolveMethod();
        var args = BuildArguments(method, inputDescriptors, context, cancellationToken);

        ApplyPropertyInputs(hostInstance, inputDescriptors, context);

        var resultValue = await InvokeAndGetResultAsync(hostInstance, method, args);
        SetOutput(activityDescriptor, resultValue, context);
    }

    private MethodInfo ResolveMethod()
    {
        var method = HostType.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            throw new InvalidOperationException($"Method '{MethodName}' not found on type '{HostType.Name}'.");

        return method;
    }

    private object?[] BuildArguments(MethodInfo method, ICollection<InputDescriptor> inputDescriptors, ActivityExecutionContext context, CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
                continue;
            }

            var inputDescriptor = inputDescriptors.FirstOrDefault(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (inputDescriptor == null)
            {
                args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
                continue;
            }

            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;
            args[i] = ConvertIfNeeded(inputValue, parameter.ParameterType);
        }

        return args;
    }

    private void ApplyPropertyInputs(object hostInstance, ICollection<InputDescriptor> inputDescriptors, ActivityExecutionContext context)
    {
        var hostPropertyLookup = HostType.GetProperties().ToDictionary(x => x.Name, x => x);

        foreach (var inputDescriptor in inputDescriptors)
        {
            if (!hostPropertyLookup.TryGetValue(inputDescriptor.Name, out var prop) || !prop.CanWrite)
                continue;

            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;
            inputValue = ConvertIfNeeded(inputValue, prop.PropertyType);

            prop.SetValue(hostInstance, inputValue);
        }
    }

    private async Task<object?> InvokeAndGetResultAsync(object hostInstance, MethodInfo method, object?[] args)
    {
        var invocationResult = method.Invoke(hostInstance, args);
        if (invocationResult is not Task task)
            throw new InvalidOperationException($"Method '{MethodName}' did not return a Task.");

        await task;

        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return null;
    }

    private void SetOutput(ActivityDescriptor activityDescriptor, object? resultValue, ActivityExecutionContext context)
    {
        var outputDescriptor = activityDescriptor.Outputs.SingleOrDefault();
        if (outputDescriptor == null)
            return;

        var output = (Output?)outputDescriptor.ValueGetter(this);
        context.Set(output, resultValue, outputDescriptor.Name);
    }

    private static object? ConvertIfNeeded(object? value, Type targetType)
    {
        if (value is ExpandoObject expandoObject)
            return expandoObject.ConvertTo(targetType);

        return value;
    }
}
