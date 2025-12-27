using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Activities.CodeFirst;
using Elsa.Workflows.Management.Attributes;
using Elsa.Workflows.Models;
using Humanizer;

namespace Elsa.Workflows.Management.Services;

public class HostMethodActivityDescriber(IActivityDescriber activityDescriber) : IHostMethodActivityDescriber
{
    public async Task<IEnumerable<ActivityDescriptor>> DescribeAsync(string key, Type hostType, CancellationToken cancellationToken = default)
    {
        var methods = hostType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
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
        var methodNameWithoutAsync = StripAsyncSuffix(methodName);
        var methodDisplayName = displayAttribute?.Name ?? methodNameWithoutAsync.Humanize().Transform(To.TitleCase);
        var displayName = !string.IsNullOrWhiteSpace(typeDisplayName) ? typeDisplayName : methodDisplayName;
        if (!string.IsNullOrWhiteSpace(activityAttribute?.DisplayName))
            displayName = activityAttribute.DisplayName!;

        descriptor.Name = methodName;
        descriptor.TypeName = activityTypeName;
        descriptor.DisplayName = displayName;
        descriptor.Description = activityAttribute?.Description ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? hostType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        descriptor.Category = activityAttribute?.Category ?? hostType.Name.Humanize().Transform(To.TitleCase);
        descriptor.Kind = activityAttribute?.Kind ?? ActivityKind.Task;
        descriptor.RunAsynchronously = activityAttribute?.RunAsynchronously ?? false;
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
            if (IsSpecialParameter(parameter))
                continue;

            // If FromServices is used, the parameter is not a workflow input unless explicitly forced via [Input].
            var isFromServices = parameter.GetCustomAttribute<FromServicesAttribute>() != null;
            var isExplicitInput = parameter.GetCustomAttribute<InputAttribute>() != null;
            if (isFromServices && !isExplicitInput)
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
        var methodName = StripAsyncSuffix(method.Name);

        if (activityAttribute != null && !string.IsNullOrWhiteSpace(activityAttribute.Namespace))
        {
            var typeSegment = activityAttribute.Type ?? methodName;
            return $"{activityAttribute.Namespace}.{typeSegment}";
        }

        return $"Elsa.Dynamic.HostMethod.{key.Pascalize()}.{methodName}";
    }

    private static string StripAsyncSuffix(string name)
    {
        return name.EndsWith("Async", StringComparison.Ordinal)
            ? name[..^5]
            : name;
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

        // No output for void or Task.
        if (returnType == typeof(void) || returnType == typeof(Task))
            return null;

        // Determine the "real" return type.
        Type actualReturnType;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            actualReturnType = returnType.GetGenericArguments()[0];
        else if (typeof(Task).IsAssignableFrom(returnType))
            return null;
        else
            actualReturnType = returnType;

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

    private static bool IsSpecialParameter(ParameterInfo parameter)
    {
        // These parameters are supplied by the runtime and should not become input descriptors.
        if (parameter.ParameterType == typeof(CancellationToken))
            return true;

        if (parameter.ParameterType == typeof(ActivityExecutionContext))
            return true;

        return false;
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