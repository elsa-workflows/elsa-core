using Elsa.JavaScript.Contracts;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.HostedServices;

/// <summary>
/// Registers variable types with the JavaScript type alias registry
/// </summary>
public class RegisterVariableTypesWithJavaScriptHostedService : IHostedService
{
    private readonly ITypeAliasRegistry _typeAliasRegistry;
    private readonly IOptions<ManagementOptions> _managementOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterVariableTypesWithJavaScriptHostedService"/> class.
    /// </summary>
    public RegisterVariableTypesWithJavaScriptHostedService(ITypeAliasRegistry typeAliasRegistry, IOptions<ManagementOptions> managementOptions)
    {
        _typeAliasRegistry = typeAliasRegistry;
        _managementOptions = managementOptions;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var variableDescriptor in _managementOptions.Value.VariableDescriptors) 
            _typeAliasRegistry.RegisterType(variableDescriptor.Type, variableDescriptor.Type.Name);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}