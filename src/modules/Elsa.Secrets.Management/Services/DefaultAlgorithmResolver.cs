using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

public class DefaultAlgorithmResolver : IAlgorithmResolver
{
    private readonly IEnumerable<IAlgorithmProvider> _providers;
    private readonly Lazy<Task<Dictionary<string, AlgorithmDescriptor>>> _algorithms;

    public DefaultAlgorithmResolver(IEnumerable<IAlgorithmProvider> providers)
    {
        _providers = providers;
        _algorithms = new(GetAlgorithms);
    }

    public async Task<SymmetricAlgorithm> ResolveAsync(string algorithmName, CancellationToken cancellationToken = default)
    {
        var lookup = await _algorithms.Value;
        var algorithm = lookup[algorithmName];
        return algorithm.Factory();
    }

    private async Task<Dictionary<string, AlgorithmDescriptor>> GetAlgorithms()
    {
        var allDescriptors = new List<AlgorithmDescriptor>();

        foreach (var provider in _providers)
        {
            var descriptors = await provider.ListAsync();
            allDescriptors.AddRange(descriptors);
        }

        return allDescriptors.ToDictionary(x => x.Name, x => x);
    }
}