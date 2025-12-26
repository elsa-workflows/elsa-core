using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Activities.CodeFirst;
using Elsa.Workflows.Models;
using Humanizer;

namespace Elsa.Workflows.Management.Services;

public class HostMethodActivityDescriber(IActivityDescriber activityDescriber) : IHostMethodActivityDescriber
{
    public async Task<IEnumerable<ActivityDescriptor>> DescribeAsync(string key, Type hostType, CancellationToken cancellationToken = default)
    {
        var methods = hostType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(IsAllowedMethod)
            .ToList();

        var descriptors = new List<ActivityDescriptor>(methods.Count);
        foreach (var method in methods)
        {
            var descriptor = await DescribeMethodAsync(key, hostType, method, cancellationToken);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    public async Task<ActivityDescriptor> DescribeMethodAsync(string key, Type hostType, MethodInfo method, CancellationToken cancellationToken = default)
    {
        var descriptor = await activityDescriber.DescribeActivityAsync(typeof(HostMethodActivity), cancellationToken);
        var activityAttribute = hostType.GetCustomAttribute<ActivityAttribute>() ?? method.GetCustomAttribute<ActivityAttribute>();

        var methodName = method.Name;
        var activityTypeName = BuildActivityTypeName(key, method, activityAttribute);

        var displayAttribute = method.GetCustomAttribute<DisplayAttribute>();
        var typeDisplayName = activityAttribute?.DisplayName ?? hostType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        var methodDisplayName = displayAttribute?.Name ?? methodName.Humanize().Transform(To.TitleCase);
        var displayName = !string.IsNullOrWhiteSpace(typeDisplayName) ? typeDisplayName : methodDisplayName;
        if (!string.IsNullOrWhiteSpace(activityAttribute?.DisplayName))
            displayName = activityAttribute.DisplayName!;

        descriptor.Name = methodName;
        descriptor.TypeName = activityTypeName;
        descriptor.DisplayName = displayName;
        descriptor.Description = activityAttribute?.Description ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? hostType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        descriptor.Category = activityAttribute?.Category ?? "Dynamic";
        descriptor.Kind = activityAttribute?.Kind ?? ActivityKind.Task;
        descriptor.RunAsynchronously = activityAttribute?.RunAsynchronously ?? true;
        descriptor.IsBrowsable = true;
        descriptor.ClrType = typeof(HostMethodActivity);

        descriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<HostMethodActivity>();
            activity.Type = activityTypeName;
            activity.HostType = hostType;
            activity.MethodName = methodName;
            activity.RunAsynchronously = descriptor.RunAsynchronously;
            return activity;
        };

        descriptor.Inputs.Clear();
        foreach (var prop in hostType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsInputProperty(prop))
                continue;

            var inputDescriptor = CreatePropertyInputDescriptor(prop);
            descriptor.Inputs.Add(inputDescriptor);
        }

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken) || parameter.ParameterType.FullName == "Elsa.Agents.AgentExecutionContext")
                continue;

            var inputDescriptor = CreateParameterInputDescriptor(parameter);
            descriptor.Inputs.Add(inputDescriptor);
        }

        descriptor.Outputs.Clear();
        var outputDescriptor = CreateOutputDescriptor(method);
        if (outputDescriptor != null)
            descriptor.Outputs.Add(outputDescriptor);

        return descriptor;
    }

    private string BuildActivityTypeName(string key, MethodInfo method, ActivityAttribute? activityAttribute)
    {
        var methodName = method.Name.EndsWith("Async", StringComparison.Ordinal)
            ? method.Name[..^5]
            : method.Name;

        if (activityAttribute != null && !string.IsNullOrWhiteSpace(activityAttribute.Namespace))
        {
            var typeSegment = activityAttribute.Type ?? methodName;
            return $"{activityAttribute.Namespace}.{typeSegment}";
        }

        return $"Elsa.Dynamic.HostMethod.{key.Pascalize()}.{methodName}";
    }

    private InputDescriptor CreatePropertyInputDescriptor(PropertyInfo prop)
    {
        var inputAttribute = prop.GetCustomAttribute<InputAttribute>();
        var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();
        var descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();

        var inputName = inputAttribute?.Name ?? prop.Name;
        var displayName = inputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? prop.Name.Humanize();
        var description = inputAttribute?.Description ?? descriptionAttribute?.Description;
        var nakedInputType = prop.PropertyType;

        return new()
        {
            Name = inputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedInputType,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
            IsSynthetic = true,
            IsWrapped = true,
            UIHint = inputAttribute?.UIHint ?? ActivityDescriber.GetUIHint(nakedInputType),
            Category = inputAttribute?.Category,
            DefaultValue = inputAttribute?.DefaultValue,
            Order = inputAttribute?.Order ?? 0,
            IsBrowsable = inputAttribute?.IsBrowsable ?? true,
            AutoEvaluate = inputAttribute?.AutoEvaluate ?? true,
            IsSerializable = inputAttribute?.IsSerializable ?? true
        };
    }

    private InputDescriptor CreateParameterInputDescriptor(ParameterInfo parameter)
    {
        var inputAttribute = parameter.GetCustomAttribute<InputAttribute>();
        var displayNameAttribute = parameter.GetCustomAttribute<DisplayNameAttribute>();

        var inputName = inputAttribute?.Name ?? parameter.Name ?? "input";
        var displayName = inputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? inputName.Humanize();
        var description = inputAttribute?.Description;
        var nakedInputType = parameter.ParameterType;

        return new()
        {
            Name = inputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedInputType,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
            IsSynthetic = true,
            IsWrapped = true,
            UIHint = inputAttribute?.UIHint ?? ActivityDescriber.GetUIHint(nakedInputType),
            Category = inputAttribute?.Category,
            DefaultValue = inputAttribute?.DefaultValue,
            Order = inputAttribute?.Order ?? 0,
            IsBrowsable = inputAttribute?.IsBrowsable ?? true,
            AutoEvaluate = inputAttribute?.AutoEvaluate ?? true,
            IsSerializable = inputAttribute?.IsSerializable ?? true
        };
    }

    private OutputDescriptor? CreateOutputDescriptor(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task))
            return null;

        Type actualReturnType;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            actualReturnType = returnType.GetGenericArguments()[0];
        else
            return null;

        if (!IsSupportedReturnType(actualReturnType))
            return null;

        var outputAttribute = method.ReturnParameter.GetCustomAttribute<OutputAttribute>() ??
                              method.GetCustomAttribute<OutputAttribute>() ??
                              method.DeclaringType?.GetCustomAttribute<OutputAttribute>();

        var displayNameAttribute = method.ReturnParameter.GetCustomAttribute<DisplayNameAttribute>();
        var outputName = outputAttribute?.Name ?? "Output";
        var displayName = outputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? outputName.Humanize();
        var description = outputAttribute?.Description ?? "The method output.";
        var nakedOutputType = actualReturnType;

        return new()
        {
            Name = outputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedOutputType,
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
            IsBrowsable = outputAttribute?.IsBrowsable ?? true,
            IsSerializable = outputAttribute?.IsSerializable ?? true
        };
    }

    private static bool IsAllowedMethod(MethodInfo method)
    {
        if (!typeof(Task).IsAssignableFrom(method.ReturnType))
            return false;

        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = method.ReturnType.GetGenericArguments()[0];
            if (!IsSupportedReturnType(resultType))
                return false;
        }

        return true;
    }

    private static bool IsSupportedReturnType(Type resultType)
    {
        var agentResponseTypeName = "Microsoft.Agents.AI.AgentRunResponse";
        return resultType == typeof(string) || resultType == typeof(object) || resultType.FullName == agentResponseTypeName;
    }

    private static bool IsInputProperty(PropertyInfo prop)
    {
        if (!prop.CanRead || !prop.CanWrite)
            return false;

        if (prop.GetIndexParameters().Length > 0)
            return false;

        return true;
    }
}
