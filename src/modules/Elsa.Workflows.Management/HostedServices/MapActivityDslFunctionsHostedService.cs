using Elsa.Dsl.Contracts;
using Elsa.Dsl.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.HostedServices;

/// <summary>
/// Registers function activity descriptors with the DSL.
/// </summary>
public class MapActivityDslFunctionsHostedService : IHostedService
{
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IDictionary<string,FunctionActivityDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapActivityDslFunctionsHostedService"/> class.
    /// </summary>
    public MapActivityDslFunctionsHostedService(IOptions<DslIntegrationOptions> options, IFunctionActivityRegistry functionActivityRegistry)
    {
        _functionActivityRegistry = functionActivityRegistry;
        _descriptors = options.Value.FunctionActivityDescriptors;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var descriptor in _descriptors.Values) 
            _functionActivityRegistry.RegisterFunction(descriptor);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}