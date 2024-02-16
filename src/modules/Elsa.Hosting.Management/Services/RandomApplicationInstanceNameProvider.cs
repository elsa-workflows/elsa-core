using Elsa.Hosting.Management.Contracts;
using Elsa.Workflows.Services;

namespace Elsa.Hosting.Management.Services;

/// <summary>
/// Returns a randomly generated instance name.
/// </summary>
public class RandomApplicationInstanceNameProvider : IApplicationInstanceNameProvider
{
    private readonly string _instanceName;
    
    public RandomApplicationInstanceNameProvider(RandomLongIdentityGenerator identityGenerator)
    {
        _instanceName = identityGenerator.GenerateId();
    }

    /// <inheritdoc />
    public string GetName() => _instanceName;
}