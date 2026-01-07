using System.Reflection;

namespace Elsa.Workflows.Management;

/// <summary>
/// Provides an extensibility hook to resolve values for host method parameters.
/// Implementations can decide to resolve from workflow inputs, DI, context, or any other source.
/// </summary>
public interface IHostMethodParameterValueProvider
{
    /// <summary>
    /// Attempts to provide a value for the specified parameter.
    /// Return a handled result when a value was provided (including <c>null</c>), otherwise return <see cref="HostMethodParameterValueProviderResult.Unhandled"/>
    /// to let other providers handle it.
    /// </summary>
    ValueTask<HostMethodParameterValueProviderResult> GetValueAsync(HostMethodParameterValueProviderContext context);
}

/// <summary>
/// Result returned by <see cref="IHostMethodParameterValueProvider"/>.
/// </summary>
public readonly record struct HostMethodParameterValueProviderResult(bool Handled, object? Value)
{
    public static HostMethodParameterValueProviderResult Unhandled { get; } = new(false, null);
    public static HostMethodParameterValueProviderResult HandledValue(object? value) => new(true, value);
}

/// <summary>
/// Context passed to <see cref="IHostMethodParameterValueProvider"/>.
/// </summary>
public record HostMethodParameterValueProviderContext(
    IServiceProvider ServiceProvider,
    ActivityExecutionContext ActivityExecutionContext,
    IReadOnlyCollection<Elsa.Workflows.Models.InputDescriptor> InputDescriptors,
    IActivity Activity,
    ParameterInfo Parameter,
    CancellationToken CancellationToken)
{
    public string ParameterName => Parameter.Name ?? string.Empty;
}
