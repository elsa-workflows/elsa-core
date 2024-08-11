using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

/// <summary>
/// Returns an instance of a concrete type associated with the specified algorithm name. 
/// </summary>
public interface IAlgorithmResolver
{
    Task<SymmetricAlgorithm> ResolveAsync(string algorithmName, CancellationToken cancellationToken = default);
}