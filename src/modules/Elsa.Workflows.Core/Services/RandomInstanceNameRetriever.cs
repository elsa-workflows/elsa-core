using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Services;

/// <summary>
/// Returns a randomly generated instance name.
/// </summary>
public class RandomInstanceNameRetriever : IInstanceNameRetriever
{
    private readonly string _instanceName;
    
    public RandomInstanceNameRetriever(RandomLongIdentityGenerator identityGenerator)
    {
        _instanceName = identityGenerator.GenerateId();
    }

    /// <inheritdoc />
    public string GetName() => _instanceName;
}