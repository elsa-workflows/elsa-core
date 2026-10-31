namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Convenience implementation of <see cref="IHostMethodParameterValueProvider"/> that delegates to a user-provided function.
/// </summary>
public class DelegateHostMethodParameterValueProvider(Func<HostMethodParameterValueProviderContext, ValueTask<HostMethodParameterValueProviderResult>> handler)
    : IHostMethodParameterValueProvider
{
    public ValueTask<HostMethodParameterValueProviderResult> GetValueAsync(HostMethodParameterValueProviderContext context) => handler(context);
}

