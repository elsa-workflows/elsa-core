using System.Security.Cryptography;

namespace Elsa.Secrets.Management;

/// <summary>
/// Returns an instance of a concrete type associated with the specified algorithm name.
/// The implementation is responsible for resolving the algorithm name to a concrete type using the provided algorithm providers.
/// </summary>
public interface IAlgorithmResolver
{
    Task<SymmetricAlgorithm> ResolveAsync(string algorithmName, CancellationToken cancellationToken = default);
}