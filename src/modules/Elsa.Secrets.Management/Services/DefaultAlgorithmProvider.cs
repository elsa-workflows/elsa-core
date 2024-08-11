using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

public class DefaultAlgorithmProvider : IAlgorithmProvider
{
    public Task<IEnumerable<AlgorithmDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = List();
        return Task.FromResult(descriptors);
    }
    
    public IEnumerable<AlgorithmDescriptor> List()
    {
        yield return new AlgorithmDescriptor(nameof(Aes), Aes.Create);
    }
}